using CommonUtils.Serializer;
using CommonUtils.Validation;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static RabbitMQWrapper.Properties.Resources;

namespace RabbitMQWrapper
{
    public sealed class QueuePublisher<T> : IQueuePublisher<T> where T : class
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QueuePublisher<T>));

        private IModel channel;
        private readonly object channelLock = new object();
        private ManualResetEventSlim channelAvailableEvent = new ManualResetEventSlim(false);

        private readonly IConnectionHandler connectionHandler;
        private readonly IQueueWrapperConfiguration queueWrapperConfig;
        private readonly string exchangeName;
        private readonly string configuredRoutingKey;
        private readonly bool publishesPersistentMessages;

        private readonly IValidationHelper validationHelper;
        private readonly ISerializer serializer;

        private IDictionary<ulong, QueueMessage<T>> unackedMessages;
        private readonly object unackedMessagesLock = new object();

        private readonly Timer publishTimeoutTimer;
        private ConcurrentDictionary<long, IEnumerable<ulong>> publishedMessageTimeline;
        private readonly long timeoutTicks;

        public QueuePublisher(
            string publisherName,
            IConnectionHandler connectionHandler,
            IQueueWrapperConfiguration configuration,
            IValidationHelper validationHelper,
            ISerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(publisherName))
                throw new ArgumentNullException(nameof(publisherName));

            if (connectionHandler == null)
                throw new ArgumentNullException(nameof(connectionHandler));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var publisherConfiguration = configuration.Publishers[publisherName];

            if (publisherConfiguration == null)
                throw new ArgumentException(string.Format(PublisherNameNotFoundError, publisherName));

            if (validationHelper == null)
                throw new ArgumentNullException(nameof(validationHelper));

            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            this.queueWrapperConfig = configuration;
            this.exchangeName = publisherConfiguration.ExchangeName;
            this.configuredRoutingKey = publisherConfiguration.RoutingKey;
            this.publishesPersistentMessages = publisherConfiguration.PublishesPersistentMessages;

            this.connectionHandler = connectionHandler;
            this.connectionHandler.ConnectionLost += ConnectionHandler_ConnectionLost;
            this.connectionHandler.ConnectionRestored += ConnectionHandler_ConnectionRestored;

            this.unackedMessages = new Dictionary<ulong, QueueMessage<T>>();

            this.validationHelper = validationHelper;

            this.serializer = serializer;

            CreateChannel();

            this.publishedMessageTimeline = new ConcurrentDictionary<long, IEnumerable<ulong>>();
            var timeoutSpan = TimeSpan.FromSeconds(configuration.ChannelConfirmTimeoutIntervalSeconds);
            this.publishTimeoutTimer = new Timer(Channel_ConfirmTimeout, null, timeoutSpan, timeoutSpan);
            this.timeoutTicks = queueWrapperConfig.PublishMessageConfirmationTimeoutSeconds * TimeSpan.TicksPerSecond;
        }

        #region IQueuePublisher
        public void Publish(T message)
        {
            Publish(message, null, null);
        }

        public void Publish(T message, string dynamicRoutingKey)
        {
            Publish(message, null, dynamicRoutingKey);
        }

        public void Publish(T message, IDictionary<string, object> headers)
        {
            Publish(message, headers, null);
        }

        public void Publish(T message, IDictionary<string, object> headers, string dynamicRoutingKey)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // Validate message
            validationHelper.Validate(message);

            // serialise object...
            string messageBody = serializer.SerializeObject(message);

            // Determine routing key
            var routingKey = dynamicRoutingKey ?? this.configuredRoutingKey;

            logger.DebugFormat(PublishingMessageLogEntry, exchangeName, routingKey, messageBody);

            // Publish Message
            lock (channelLock)
            {
                // Create message properties
                var basicProperties = CreateBasicProperties(headers);

                ulong sequenceNumber = channel.NextPublishSeqNo;
                var queueMessage = new QueueMessage<T>(message, sequenceNumber, routingKey, headers);

                // Store this message in the unacked store
                lock (unackedMessagesLock)
                {
                    this.unackedMessages[sequenceNumber] = queueMessage;
                }

                // Add to the timeline
                this.publishedMessageTimeline.AddOrUpdate(DateTime.UtcNow.Ticks,
                    new ulong[] { sequenceNumber },
                    (ticks, existing) => existing.Union(new ulong[] { sequenceNumber }).ToArray());

                try
                {
                    WaitForConnection();
                    channel?.BasicPublish(
                        this.exchangeName,
                        routingKey,
                        true,
                        basicProperties,
                        Encoding.UTF8.GetBytes(messageBody));
                }
                catch (AlreadyClosedException)
                {
                    // The channel has been closed. Log an error and recreate the channel.
                    logger.ErrorFormat(ChannelClosedLogEntry, channel.CloseReason);
                    this.channelAvailableEvent.Reset();
                    CreateChannel();
                }
                catch (IOException e)
                {
                    logger.Warn(PublishingConnectionClosedLogEntry, e);
                }
            }
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.publishTimeoutTimer.Dispose();

                    if (this.channel != null)
                    {
                        this.channel.Close();
                        this.channel.Dispose();
                        this.channel = null;
                    }

                    if (this.channelAvailableEvent != null)
                    {
                        this.channelAvailableEvent.Dispose();
                        this.channelAvailableEvent = null;
                    }
                }

                disposed = true;
            }
        }
        #endregion
        #endregion

        #region Helper Methods
        private void CreateChannel()
        {
            if (!channelAvailableEvent.IsSet)
            {
                try
                {
                    this.channel = connectionHandler.CreateModel();
                    this.channel.BasicAcks += Channel_BasicAcks;
                    this.channel.BasicNacks += Channel_BasicNacks;
                    this.channel.BasicReturn += Channel_BasicReturn;
                    this.channel.ConfirmSelect();

                    channelAvailableEvent.Set();
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to CreateChannel error details - '{ex.Message}'.");
                }
            }
        }

        private void WaitForConnection()
        {
            channelAvailableEvent?.Wait();
        }

        private IBasicProperties CreateBasicProperties(IDictionary<string, object> headers)
        {
            var basicProperties = this.channel.CreateBasicProperties();
            basicProperties.AppId = $"{GlobalContext.Properties["COMPONENT-NAME"]}_{Environment.MachineName}";

            if (serializer.GetType() == typeof(XmlSerializer))
            {
                basicProperties.ContentType = "application/xml";
            }
            else
            {
                basicProperties.ContentType = "application/json";
            }

            if (headers != null)
            {
                basicProperties.Headers = headers;
            }

            basicProperties.Persistent = this.publishesPersistentMessages;
            basicProperties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            return basicProperties;
        }

        private void ConnectionHandler_ConnectionRestored(object sender, EventArgs e)
        {
            if (!channelAvailableEvent.IsSet)
            {
                CreateChannel();
            }
        }

        private void ConnectionHandler_ConnectionLost(object sender, EventArgs e)
        {
            if (channelAvailableEvent.IsSet)
            {
                // re-initialise the publisher timeline and unacked messages dictionary following connection loss to prevent resending old messages
                this.publishedMessageTimeline = new ConcurrentDictionary<long, IEnumerable<ulong>>();
                lock (unackedMessagesLock)
                {
                    this.unackedMessages = new Dictionary<ulong, QueueMessage<T>>();
                }

                channelAvailableEvent.Reset();
            }
        }

        private void Channel_BasicReturn(object sender, RabbitMQ.Client.Events.BasicReturnEventArgs e)
        {
            logger.ErrorFormat(MessageReturnedLogEntry, e.Exchange, e.RoutingKey, e.ReplyText);
        }
        #endregion

        #region Publish Confirms
        private void Channel_BasicNacks(object sender, RabbitMQ.Client.Events.BasicNackEventArgs e)
        {
            logger.InfoFormat(NegativePublisherConfirmReceivedLogEntry, e.DeliveryTag, e.Multiple);
            HandleConfirmResponse(e.DeliveryTag, e.Multiple, message =>
            {
                if (String.IsNullOrEmpty(message.RoutingKey))
                {
                    Publish(message.Message, message.MessageHeaders);
                }
                else
                {
                    Publish(message.Message, message.MessageHeaders, message.RoutingKey);
                }
            });
        }

        private void Channel_BasicAcks(object sender, RabbitMQ.Client.Events.BasicAckEventArgs e)
        {
            logger.DebugFormat(PublisherConfirmReceivedLogEntry, e.DeliveryTag, e.Multiple);
            HandleConfirmResponse(e.DeliveryTag, e.Multiple);
        }

        private void Channel_ConfirmTimeout(object state)
        {
            var nowInTicks = DateTime.UtcNow.Ticks;
            var keys = publishedMessageTimeline.Keys
                        .Where(k => k + timeoutTicks <= nowInTicks)
                        .ToArray();

            foreach (var key in keys)
            {
                IEnumerable<ulong> deliveryTags;
                if (publishedMessageTimeline.TryRemove(key, out deliveryTags))
                {
                    foreach (var deliveryTag in deliveryTags)
                    {
                        HandleConfirmResponse(deliveryTag, false, message =>
                        {
                            logger.InfoFormat(PublishMessageTimeoutLogEntry, deliveryTag);
                            if (String.IsNullOrEmpty(message.RoutingKey))
                            {
                                Publish(message.Message, message.MessageHeaders);
                            }
                            else
                            {
                                Publish(message.Message, message.MessageHeaders, message.RoutingKey);
                            }
                        });
                    }
                }
            }
        }

        private void HandleConfirmResponse(ulong deliveryTag, bool multiple, Action<QueueMessage<T>> confirmAction = null)
        {
            var queueMessagesToConfirm = new List<QueueMessage<T>>();

            if (multiple)
            {
                lock (unackedMessagesLock)
                {
                    // have to create a new list to enumerate over so we are not trying to modify the collection we are enumerating.
                    var tagsToBeConfirmed = new List<ulong>();
                    tagsToBeConfirmed.AddRange(unackedMessages.Keys.Where(k => k <= deliveryTag));

                    foreach (var tag in tagsToBeConfirmed)
                    {
                        queueMessagesToConfirm.Add(unackedMessages[tag]);
                        unackedMessages.Remove(tag);
                    }
                }
            }
            else
            {
                lock (unackedMessagesLock)
                {
                    if (unackedMessages.ContainsKey(deliveryTag))
                    {
                        queueMessagesToConfirm.Add(unackedMessages[deliveryTag]);
                        unackedMessages.Remove(deliveryTag);
                    }
                }
            }

            if (confirmAction != null)
            {
                foreach (var queueMessage in queueMessagesToConfirm)
                {
                    confirmAction(queueMessage);
                }
            }
        }
        #endregion
    }
}