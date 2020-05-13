using CommonUtils.Exceptions;
using CommonUtils.Logging;
using CommonUtils.Threading;
using log4net;
using RabbitMQWrapper.Enumerations;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using RabbitMQWrapper.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper
{
    /// <summary>
    /// An event listener that will process messages with the same initial routing key sequentially on the same thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SequentialProcessingEventListener<T> where T : class
    {
        private sealed class ProcessingQueue
        {
            public ConcurrentQueue<QueueMessage<T>> Queue { get; set; }

            public AutoResetEventAsync AutoResetEvent { get; set; }

            public ProcessingQueue()
            {
                Queue = new ConcurrentQueue<QueueMessage<T>>();
                AutoResetEvent = new AutoResetEventAsync();
            }
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(SequentialProcessingEventListener<T>));

        private readonly IQueueConsumer<T> queueConsumer;
        private readonly string performanceLoggingMethodName;

        protected SequentialProcessingEventListener(IQueueConsumer<T> queueConsumer)
        {
            if (queueConsumer == null)
                throw new ArgumentNullException(nameof(queueConsumer));

            this.queueConsumer = queueConsumer;
            this.performanceLoggingMethodName = GetType().Name + "." + nameof(ProcessMessageAsync);
        }
        /// <summary>
        /// The message acknowledgement strategy for this Event Listener.
        /// </summary>
        protected virtual AcknowledgeBehaviour Behaviour => AcknowledgeBehaviour.AfterProcess;

        /// <summary>
        /// Process a message from a queue
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <param name="deliveryTag">The message delivery tag; useful for logging.</param>
        /// <param name="routingKey">The routing key that was used to deliver this message.</param>
        /// <param name="cancellationToken">A cancellation token to be observed.</param>
        /// <returns></returns>
        protected abstract Task ProcessMessageAsync(T message, ulong deliveryTag, string routingKey, CancellationToken cancellationToken);

        protected virtual string GetProcessingSequenceIdentifier(string routingKey)
        {
            if (string.IsNullOrWhiteSpace(routingKey))
                throw new ArgumentNullException(nameof(routingKey));

            string[] routingKeyParts = routingKey.Split('.');
            return routingKeyParts[0];
        }

        /// <summary>
        /// Start listening to the queue and process its messages in a sequential fashion
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Run(CancellationToken cancellationToken)
        {
            var processingQueues = new Dictionary<string, ProcessingQueue>();
            var tasks = new List<Task>();
            QueueMessage<T> queueMessage;

            while (!cancellationToken.IsCancellationRequested && !tasks.Any(t => t.IsFaulted))
            {
                if (queueConsumer.TryGetNextMessage(queueConsumer.Configuration.MessageWaitTimeoutMilliseconds, out queueMessage)
                    && !cancellationToken.IsCancellationRequested)
                {
                    string processingSequenceIdentifier = GetProcessingSequenceIdentifier(queueMessage.RoutingKey);

                    if (processingQueues.ContainsKey(processingSequenceIdentifier))
                    {
                        // Add a message to the processing queue, and signal the processing thread to alert it to the new message.
                        var processingQueue = processingQueues[processingSequenceIdentifier];
                        processingQueue.Queue.Enqueue(queueMessage);
                        processingQueue.AutoResetEvent.Set();
                    }
                    else
                    {
                        // create a new processing queue and kick off a task to process it.
                        var processingQueue = new ProcessingQueue();
                        processingQueue.Queue.Enqueue(queueMessage);
                        processingQueues[processingSequenceIdentifier] = processingQueue;
                        var t = Task.Run(RunSequentialProcessor(processingQueue, cancellationToken));
                        tasks.Add(t);
                    }

                    // Remove completed queues
                    var processingQueuesToRemove = new List<string>();
                    foreach (var processingQueue in processingQueues)
                    {
                        if (!processingQueue.Value.Queue.Any())
                        {
                            processingQueue.Value.AutoResetEvent.Set();
                            processingQueuesToRemove.Add(processingQueue.Key);
                        }
                    }

                    foreach (var processingQueueToRemove in processingQueuesToRemove)
                    {
                        processingQueues.Remove(processingQueueToRemove);
                    }

                    // Remove completed tasks
                    tasks.RemoveAll(x => x.IsCompleted);
                }
            }

            // Exit all processing threads
            foreach (var processingQueue in processingQueues.Values)
            {
                processingQueue.AutoResetEvent.Set();
            }

            Task.WaitAll(tasks.ToArray());
            // Note: Don't need to dispose of tasks. See http://blogs.msdn.com/b/pfxteam/archive/2012/03/25/10287435.aspx
        }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private Func<Task> RunSequentialProcessor(ProcessingQueue processingQueue, CancellationToken cancellationToken)
        {
            return async () =>
            {
                QueueMessage<T> dequeuedMessage;
                while (!cancellationToken.IsCancellationRequested && processingQueue.Queue.TryPeek(out dequeuedMessage))
                {
                    using (var performanceLogger = new PerformanceLogger(performanceLoggingMethodName))
                    {
                        logger.DebugFormat(Resources.MessageReceivedLogEntry, dequeuedMessage.DeliveryTag);

                        try
                        {
                            await ProcessMessageAsync(
                                dequeuedMessage.Message,
                                dequeuedMessage.DeliveryTag,
                                dequeuedMessage.RoutingKey,
                                cancellationToken);

                            if (Behaviour != AcknowledgeBehaviour.Async)
                                queueConsumer.AcknowledgeMessage(dequeuedMessage.DeliveryTag);

                            logger.InfoFormat(Resources.MessageProcessedLogEntry, dequeuedMessage.DeliveryTag);
                        }
                        catch (FatalErrorException e)
                        {
                            logger.Fatal(Resources.FatalErrorLogEntry, e);
                            queueConsumer.NegativelyAcknowledgeAndRequeue(dequeuedMessage.DeliveryTag);
                            throw;
                        }
                        catch (Exception e)
                        {
                            logger.ErrorFormat(Resources.ProcessingErrorLogEntry, dequeuedMessage.DeliveryTag, e);
                            queueConsumer.NegativelyAcknowledge(dequeuedMessage.DeliveryTag);
                        }
                    }

                    // we have finished processing - remove the message from the queue and wait for the next one.
                    processingQueue.Queue.TryDequeue(out dequeuedMessage);
                    await processingQueue.AutoResetEvent.WaitOneAsync();
                }
            };
        }
    }
}
