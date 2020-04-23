﻿using Covid.CommonUtils.Serializers;
using Covid.Rabbit.Configuration;
using Covid.Rabbit.Connection;
using Covid.Rabbit.Factories;
using log4net;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Covid.Rabbit.Publisher
{
    public sealed class QueuePublisher<T> where T : class
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(QueuePublisher<>));
        private readonly IQueueConnectionFactory _connectionFactory;
        private readonly IJsonSerializer _serializer;
        private readonly string _publisherName;
        private readonly IQueueConfig _queueConfig;
        private readonly CancellationToken _cancellationToken;
        private bool _connected;
        private readonly object _lock = new object();
        private IConnectionHandler _connection;
        private IModel _channel;

        public QueuePublisher(IQueueConfiguration queueConfiguration,
                              IQueueConnectionFactory connectionFactory,
                              IJsonSerializer serializer,
                              string publisherName,
                              CancellationToken cancellationToken)
        {
            if (queueConfiguration == null)
                throw new ArgumentNullException(nameof(queueConfiguration));

            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _publisherName = publisherName ?? throw new ArgumentNullException(nameof(publisherName));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cancellationToken = cancellationToken;

            // retrieve the specific queues configuration
            _queueConfig = queueConfiguration.Publishers.Cast<QueueConfig>().Where(p => p.Name == _publisherName).FirstOrDefault() as IQueueConfig;

            if (_queueConfig == null)
                throw new ArgumentNullException(nameof(_queueConfig));
        }

        public void Publish(T message, string routingKey = null)
        {
            // TODO connection persistence, stop lots of connections
            try
            {
                if (!_connected)
                {
                    _connection = _connectionFactory.CreateConnection(_queueConfig.Name, _cancellationToken);

                    _channel = _connection.CreateModel();

                    lock (_lock)
                    {
                        _connected = true;
                    }
                }

                var body = Encoding.UTF8.GetBytes(_serializer.SerializeObject(message));

                _channel.BasicPublish(exchange: _queueConfig.Exchange,
                                     routingKey: !string.IsNullOrEmpty(routingKey) ? routingKey : _queueConfig.RoutingKey,
                                     basicProperties: null,
                                     body: body);
                _logger.Info($"Sent message");

            }
            catch (Exception ex)
            {
                _logger.Error($"An unexpected exception occurred, error details '{ex.Message}'", ex);

                lock (_lock)
                {
                    _connected = false;
                }

                if (_channel != null)
                    _channel.Dispose();

                if (_connection != null)
                    _connection.Dispose();
            }
        }
    }
}
