﻿using Covid.Rabbit.Configuration;
using Covid.Rabbit.Connection;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Covid.Rabbit.Factories
{
    public class QueueConnectionFactory : IQueueConnectionFactory
    {
        private bool isDisposed;
        private readonly IQueueConfiguration _queueConfiguration;
        private readonly ConcurrentDictionary<string, IConnectionHandler> _connections;
        private readonly IConnectionFactory _connectionFactory;
        private readonly object _lock = new object();


        public QueueConnectionFactory(IQueueConfiguration queueConfiguration)
        {
            _queueConfiguration = queueConfiguration ?? throw new ArgumentNullException(nameof(QueueConnectionFactory));
            _connections = new ConcurrentDictionary<string, IConnectionHandler>();
            _connectionFactory = new ConnectionFactory()
            {
                HostName = _queueConfiguration.Hostname,
                UserName = _queueConfiguration.Username,
                Password = _queueConfiguration.Password,
                // connection that will recover automatically
                AutomaticRecoveryEnabled = _queueConfiguration.AutomaticRecoveryEnabled,
                // attempt recovery every 10 seconds
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_queueConfiguration.NetworkRecoveryIntervalSeconds),
                ContinuationTimeout = TimeSpan.FromSeconds(_queueConfiguration.ContinuationTimeoutSeconds),
                HandshakeContinuationTimeout = TimeSpan.FromSeconds(_queueConfiguration.HandshakeContinuationTimeoutSeconds),
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(_queueConfiguration.RequestedConnectionTimeoutSeconds * 60),
                RequestedHeartbeat = TimeSpan.FromSeconds(_queueConfiguration.RequestedHeartbeatSeconds),
                Ssl = new SslOption(_queueConfiguration.Hostname, _queueConfiguration.CertificatePath, _queueConfiguration.CertificatePath != null),
                Uri = !string.IsNullOrEmpty(_queueConfiguration.Uri) ? new Uri(_queueConfiguration.Uri) : null,
                Port = _queueConfiguration.Port.HasValue ? _queueConfiguration.Port.Value : AmqpTcpEndpoint.UseDefaultPort,
            };
        }

        public IConnectionHandler CreateConnection(string connectionName, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (!_connections.ContainsKey(connectionName))
                {
                    var _connectionHandler = new ConnectionHandler(connectionName, _connectionFactory.CreateConnection(), cancellationToken, _queueConfiguration.AutomaticRecoveryEnabled);

                    _connections.TryAdd(connectionName, _connectionHandler);
                }

                return _connections[connectionName];
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                if (_connections != null && _connections.Any())
                {
                    foreach (var connection in _connections)
                    {
                        lock (_lock)
                        {
                            connection.Value.Dispose();
                            _connections.TryRemove(connection.Key, out _);
                        }
                    }
                }
            }

            isDisposed = true;
        }
    }
}
