using CommonUtils.Validation;
using log4net;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using System;
using System.IO;
using System.Text;
using System.Threading;
using static RabbitMQWrapper.Properties.Resources;

namespace RabbitMQWrapper
{
    public sealed class QueueConsumer<T> : IQueueConsumer<T> where T : class
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QueueConsumer<T>));

        private readonly IConnectionHandler connectionHandler;
        private readonly IConsumerConfiguration consumerConfiguration;
        private readonly IQueueWrapperConfiguration queueWrapperConfiguration;
        private readonly IValidationHelper validationHelper;
        private readonly ushort messagePrefetchCount;

        private IModel channel;
        private readonly object channelLock = new object();
        private readonly ManualResetEventSlim channelAvailableEvent = new ManualResetEventSlim(false);
        private QueueingBasicConsumer consumer;
        private string queueName;


        public IConsumerConfiguration Configuration
        {
            get
            {
                return consumerConfiguration;
            }
        }

        public QueueConsumer(string consumerName, IConnectionHandler connectionHandler, IQueueWrapperConfiguration configuration, IValidationHelper validationHelper)
        {
            if (string.IsNullOrWhiteSpace(consumerName))
                throw new ArgumentNullException(nameof(consumerName));

            if (connectionHandler == null)
                throw new ArgumentNullException(nameof(connectionHandler));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            queueWrapperConfiguration = configuration;
            consumerConfiguration = configuration.Consumers[consumerName];

            if (consumerConfiguration == null)
                throw new ArgumentException(string.Format(ConsumerNameNotFoundError, consumerName));

            if (validationHelper == null)
                throw new ArgumentNullException(nameof(validationHelper));

            this.connectionHandler = connectionHandler;
            this.connectionHandler.ConnectionLost += ConnectionHandler_ConnectionLost;
            this.connectionHandler.ConnectionRestored += ConnectionHandler_ConnectionRestored;

            messagePrefetchCount = queueWrapperConfiguration.MessagePrefetchCount;

            this.validationHelper = validationHelper;

            CreateConsumer();
        }

        public bool TryGetNextMessage(int timeoutMilliseconds, out QueueMessage<T> message)
        {
            bool success = false;
            message = null;

            BasicDeliverEventArgs rabbitMessage = null;
            bool messageDequeued = false;

            try
            {
                lock (channelLock)
                {
                    if (WaitForConnection(timeoutMilliseconds))
                    {
                        messageDequeued = consumer.Queue.Dequeue(timeoutMilliseconds, out rabbitMessage);
                    }
                }
            }
            catch (EndOfStreamException e)
            {
                //if not shutting down and not lost connection then try reboot
                if (!connectionHandler.ConnectionShuttingDown && channelAvailableEvent.IsSet)
                {
                    logger.Warn(ConsumingConnectionClosedLogEntry, e);
                    //Something's gone badly wrong - perhaps due to a queue being deleted?
                    //Let's pretend the connection was closed, then wait a bit and try to reconnect.
                    ConnectionHandler_ConnectionLost(this, EventArgs.Empty);
                    WaitForConnection(queueWrapperConfiguration.MillisecondsBetweenConnectionRetries);
                    ConnectionHandler_ConnectionRestored(this, EventArgs.Empty);
                }
            }

            if (messageDequeued)
            {
                T messageObject = null;

                try
                {
                    // Deserialize object
                    messageObject = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(rabbitMessage.Body));
                    // Validate object
                    string validationErrors;
                    success = validationHelper.TryValidate(messageObject, out validationErrors);

                    if (success)
                    {
                        logger.InfoFormat(MessageSuccessfullyReceivedLogEntry, rabbitMessage.DeliveryTag, queueName);
                        message = new QueueMessage<T>(messageObject,
                            rabbitMessage.DeliveryTag,
                            rabbitMessage.RoutingKey,
                            rabbitMessage.BasicProperties.Headers);
                    }
                    else
                    {
                        logger.ErrorFormat(
                            MessageFailsValidationLogEntry,
                            rabbitMessage.DeliveryTag,
                            queueName,
                            validationErrors);
                        NegativelyAcknowledge(rabbitMessage.DeliveryTag);
                    }
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(
                        MessageFailsValidationLogEntry,
                        rabbitMessage.DeliveryTag,
                        queueName,
                        ex.Message);

                    NegativelyAcknowledge(rabbitMessage.DeliveryTag);
                }
            }

            return success;
        }

        public void AcknowledgeMessage(ulong deliveryTag)
        {
            lock (channelLock)
            {
                // if we lose connection acknowledging a delivery tag that 
                // rabbit does not know about causes the client to stop the consumer
                // so check the connection is still alive
                if (channelAvailableEvent.IsSet)
                {
                    this.channel.BasicAck(deliveryTag, false);
                }
            }

            logger.DebugFormat(MessageAcknowledgedLogEntry, deliveryTag, queueName);
        }

        public void NegativelyAcknowledge(ulong deliveryTag)
        {
            try
            {
                lock (channelLock)
                {
                    // if we lose connection acknowledging a delivery tag that 
                    // rabbit does not know about causes the client to stop the consumer
                    // so check the connection is still alive
                    if (channelAvailableEvent.IsSet)
                    {
                        this.channel.BasicNack(deliveryTag, false, false);
                    }
                }

                logger.DebugFormat(MessageNegativelyAcknowledgedLogEntry, deliveryTag, queueName);
            }
            catch (Exception e)
            {
                // Do nothing - we don't mind that the nack has failed. Rabbit will fix this if necessary.
                logger.WarnFormat("Failed to negatively acknowledge message {0} from queue {1}. {2}", deliveryTag, queueName, e);
            }
        }

        public void NegativelyAcknowledgeAndRequeue(ulong deliveryTag)
        {
            lock (channelLock)
            {
                // if we lose connection acknowledging a delivery tag that 
                // rabbit does not know about causes the client to stop the consumer
                // so check the connection is still alive
                if (channelAvailableEvent.IsSet)
                {
                    this.channel.BasicNack(deliveryTag, false, true);
                }
            }

            logger.DebugFormat(MessageRequeuedLogEntry, deliveryTag, queueName);
        }

        #region Helper Methods
        private void CreateConsumer()
        {
            if (!channelAvailableEvent.IsSet)
            {
                this.channel = connectionHandler.CreateModel();
                this.channel.BasicQos(0, messagePrefetchCount, false);

                QueueDeclareOk result;

                if (string.IsNullOrWhiteSpace(consumerConfiguration.QueueName))
                {
                    string temporaryQueueName = queueWrapperConfiguration.TemporaryQueueNamePrefix + Guid.NewGuid().ToString();
                    result = channel.QueueDeclare(temporaryQueueName, true, true, true, null);

                    if (result == null)
                    {
                        throw new IOException(TemporaryQueueCreationError);
                    }

                    queueName = result.QueueName;
                    channel.QueueBind(
                        queueName,
                        consumerConfiguration.ExchangeName,
                        consumerConfiguration.RoutingKey);
                }
                else
                {
                    queueName = consumerConfiguration.QueueName;
                }

                this.consumer = new QueueingBasicConsumer(channel);

                string consumerTag = channel.BasicConsume(queueName, false, consumer);
                logger.InfoFormat(ConsumptionStartedLogEntry, queueName, consumerTag);

                channelAvailableEvent.Set();
            }
        }

        private bool WaitForConnection(int timeoutMilliseconds = Timeout.Infinite)
        {
            return channelAvailableEvent.Wait(timeoutMilliseconds);
        }

        private void ConnectionHandler_ConnectionRestored(object sender, EventArgs e)
        {
            if (!channelAvailableEvent.IsSet)
            {
                if (this.channel != null)
                {
                    lock (channelLock)
                    {
                        if (this.channel != null)
                        {
                            this.channel.Close();
                            this.channel.Dispose();
                            this.channel = null;
                        }
                    }
                }

                if (this.channel == null)
                {
                    lock (channelLock)
                    {
                        if (this.channel == null)
                        {
                            CreateConsumer();
                        }
                    }
                }
            }
        }

        private void ConnectionHandler_ConnectionLost(object sender, EventArgs e)
        {
            if (channelAvailableEvent.IsSet)
            {
                channelAvailableEvent.Reset();
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            this.Dispose(true);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.channel != null)
                    {
                        this.channel.Close();
                        this.channel.Dispose();
                        this.channel = null;
                    }

                    this.channelAvailableEvent.Dispose();
                }

                disposed = true;
            }
        }
        #endregion
    }
}
