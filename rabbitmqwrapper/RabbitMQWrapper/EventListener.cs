
using CommonUtils.Exceptions;
using CommonUtils.Logging;
using log4net;
using RabbitMQWrapper.Enumerations;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using RabbitMQWrapper.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper
{
    public abstract class EventListener<T> where T : class
    {
        #region Private Fields
        private static readonly ILog logger = LogManager.GetLogger(typeof(EventListener<T>));

        private readonly IQueueConsumer<T> queueConsumer;
        private readonly string performanceLoggingMethodName;
        #endregion

        #region Constructor
        protected EventListener(IQueueConsumer<T> queueConsumer)
        {
            if (queueConsumer == null)
                throw new ArgumentNullException(nameof(queueConsumer));

            this.queueConsumer = queueConsumer;
            this.performanceLoggingMethodName = GetType().Name + "." + nameof(ProcessMessageAsync);
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// The message acknowledgement strategy for this Event Listener.
        /// </summary>
        protected virtual AcknowledgeBehaviour Behaviour => AcknowledgeBehaviour.AfterProcess;

        /// <summary>
        /// Process a message from a queue
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <param name="deliveryTag">The message delivery tag; useful for logging.</param>
        /// <param name="cancellationToken">A cancellation token to be observed.</param>
        protected abstract Task ProcessMessageAsync(T message, ulong deliveryTag, CancellationToken cancellationToken);

        /// <summary>
        /// Start listening to the queue and process its messages
        /// </summary>
        /// <param name="cancellationToken"></param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Run(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            QueueMessage<T> queueMessage;

            while (!cancellationToken.IsCancellationRequested && !tasks.Any(t => t.IsFaulted))
            {
                if (queueConsumer.TryGetNextMessage(queueConsumer.Configuration.MessageWaitTimeoutMilliseconds, out queueMessage)
                    && !cancellationToken.IsCancellationRequested)
                {
                    var dequeuedMessage = queueMessage;

                    var t = Task.Run(async () =>
                    {
                        using (var performanceLogger = new PerformanceLogger(performanceLoggingMethodName))
                        {
                            logger.DebugFormat(Resources.MessageReceivedLogEntry, dequeuedMessage.DeliveryTag);

                            try
                            {
                                if (Behaviour == AcknowledgeBehaviour.BeforeProcess)
                                    queueConsumer.AcknowledgeMessage(dequeuedMessage.DeliveryTag);

                                await ProcessMessageAsync(dequeuedMessage.Message, dequeuedMessage.DeliveryTag, cancellationToken);

                                if (Behaviour == AcknowledgeBehaviour.AfterProcess)
                                    queueConsumer.AcknowledgeMessage(dequeuedMessage.DeliveryTag);

                                if (Behaviour != AcknowledgeBehaviour.Never)
                                    logger.InfoFormat(Resources.MessageProcessedLogEntry, dequeuedMessage.DeliveryTag);
                            }
                            catch (FatalErrorException e)
                            {
                                if (Behaviour == AcknowledgeBehaviour.AfterProcess
                                 || Behaviour == AcknowledgeBehaviour.Async)
                                    queueConsumer.NegativelyAcknowledgeAndRequeue(dequeuedMessage.DeliveryTag);

                                logger.Fatal(Resources.FatalErrorLogEntry, e);
                                throw;
                            }
                            catch (Exception e)
                            {
                                logger.ErrorFormat(Resources.ProcessingErrorLogEntry, dequeuedMessage.DeliveryTag, e);

                                if (Behaviour == AcknowledgeBehaviour.AfterProcess
                                 || Behaviour == AcknowledgeBehaviour.Async)
                                    queueConsumer.NegativelyAcknowledge(dequeuedMessage.DeliveryTag);
                            }
                        }
                    });

                    tasks.Add(t);
                    tasks.RemoveAll(x => x.IsCompleted);
                }
            }

            Task.WaitAll(tasks.ToArray());
            // Note: Don't need to dispose of tasks. See http://blogs.msdn.com/b/pfxteam/archive/2012/03/25/10287435.aspx
        }
        #endregion

        /// <summary>
        /// Event Handler for acknowledging messages that are processed with an Async AcknowledgeBehaviour
        /// To use expose as EventHandler from a processor, etc, and attach this method to it.  
        /// See RabbitIntegration.TestMessageEventListenerAsync for example.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="eventArgs">The eventArgs</param>
        public void OnAcknowledgeMessage(object sender, AcknowledgeEventArgs eventArgs)
        {
            // check whether the eventArge contain an exception
            if (eventArgs.Exception != null)
            {
                if (eventArgs.Exception is FatalErrorException)
                {
                    eventArgs?.DeliveryTags?.ToList().ForEach(tag =>
                    {
                        queueConsumer?.NegativelyAcknowledgeAndRequeue(tag);
                    });

                    logger.Fatal(Resources.FatalErrorLogEntry, eventArgs.Exception);
                    throw eventArgs.Exception;
                }

                eventArgs?.DeliveryTags?.ToList().ForEach(tag =>
                {
                    queueConsumer?.NegativelyAcknowledge(tag);
                });
                return;
            }

            if (Behaviour == AcknowledgeBehaviour.Never)
                return;

            // acknowledge the messages
            eventArgs?.DeliveryTags?.ToList().ForEach(deliveryTag =>
            {
                queueConsumer?.AcknowledgeMessage(deliveryTag);
                logger.InfoFormat(Resources.MessageProcessedLogEntry, deliveryTag);
            });
        }
    }
}
