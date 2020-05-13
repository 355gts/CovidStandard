using CommonUtils.Certificates;
using CommonUtils.Exceptions;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQWrapper.Interfaces;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using static RabbitMQWrapper.Properties.Resources;

namespace RabbitMQWrapper
{
    public sealed class ConnectionHandler : IConnectionHandler
    {
        private readonly static ILog logger = LogManager.GetLogger(typeof(ConnectionHandler));

        private readonly ConnectionFactory connectionFactory;
        private readonly IQueueWrapperConfiguration queueWrapperConfig;
        private readonly CancellationToken cancellationToken;

        private readonly ManualResetEventSlim connectedEvent = new ManualResetEventSlim(false);
        private IConnection connection;

        internal ConnectionHandler(
            ConnectionFactory connectionFactory,
            IQueueWrapperConfiguration queueWrapperConfig,
            ICertificateHelper certificateHelper,
            CancellationToken cancellationToken)
        {
            if (connectionFactory == null)
                throw new ArgumentNullException(nameof(connectionFactory));

            if (queueWrapperConfig == null)
                throw new ArgumentNullException(nameof(queueWrapperConfig));

            if (certificateHelper == null)
                throw new ArgumentNullException(nameof(certificateHelper));

            this.queueWrapperConfig = queueWrapperConfig;

            this.connectionFactory = connectionFactory;
            this.connectionFactory.AuthMechanisms = new[] { new ExternalMechanismFactory() };
            this.connectionFactory.Uri = queueWrapperConfig.Uri.AbsoluteUri;
            this.connectionFactory.Ssl.ServerName = queueWrapperConfig.Uri.Host;
            this.connectionFactory.ContinuationTimeout = TimeSpan.FromSeconds(queueWrapperConfig.ProtocolTimeoutIntervalSeconds);
            this.connectionFactory.HandshakeContinuationTimeout = TimeSpan.FromSeconds(queueWrapperConfig.ProtocolTimeoutIntervalSeconds);

            X509Certificate2Collection certificates;
            if (!certificateHelper.TryFindCertificate(queueWrapperConfig.ClientCertificateSubjectName, out certificates))
            {
                throw new FatalErrorException(
                    string.Format(CouldNotFindCertificateError, queueWrapperConfig.ClientCertificateSubjectName, certificates.Count));
            }

            this.connectionFactory.Ssl.Certs = certificates;
            this.connectionFactory.Ssl.Version = System.Security.Authentication.SslProtocols.Tls12;
            this.connectionFactory.Ssl.Enabled = true;
            this.connectionFactory.AutomaticRecoveryEnabled = queueWrapperConfig.AutomaticRecoveryEnabled;
            this.connectionFactory.NetworkRecoveryInterval = TimeSpan.FromSeconds(queueWrapperConfig.NetworkRecoveryIntervalSeconds);
            this.connectionFactory.RequestedHeartbeat = queueWrapperConfig.RabbitMQHeartbeatSeconds;

            this.ConnectionShuttingDown = false;

            this.cancellationToken = cancellationToken;
            this.cancellationToken.Register(ConnectionCancelled);

            CreateConnection();
        }

        public ConnectionHandler(
            IQueueWrapperConfiguration queueWrapperConfig,
            ICertificateHelper certificateHelper,
            CancellationToken cancellationToken)
            : this(new ConnectionFactory(),
                    queueWrapperConfig,
                    certificateHelper,
                    cancellationToken)
        { }

        #region IConnectionWrapper
        public IModel CreateModel()
        {
            logger.Debug(CreatingModelLogEntry);
            connectedEvent.Wait(this.cancellationToken);
            return this.connection.CreateModel();
        }

        public bool ConnectionShuttingDown { get; private set; }

        public event EventHandler ConnectionLost;

        public event EventHandler ConnectionRestored;

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
                    if (this.connection != null)
                    {
                        this.connection.Dispose();
                        this.connection = null;
                    }

                    this.connectedEvent.Dispose();
                }

                disposed = true;
            }
        }
        #endregion
        #endregion

        #region Helper Methods
        private void CreateConnection()
        {
            while (!connectedEvent.IsSet && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    logger.Debug(CreatingConnectionLogEntry);
                    this.connection = connectionFactory.CreateConnection();
                    this.connection.ConnectionShutdown += (s, e) => OnConnectionLost();
                    OnConnectionRestored();
                }
                catch (BrokerUnreachableException e)
                {
                    logger.WarnFormat(ConnectionFailedLogEntry, queueWrapperConfig.MillisecondsBetweenConnectionRetries, e);
                    cancellationToken.WaitHandle.WaitOne(queueWrapperConfig.MillisecondsBetweenConnectionRetries);
                }
            }
        }

        private void OnConnectionLost()
        {
            if (connectedEvent.IsSet)
            {
                logger.Debug(ConnectionLostLogEntry);

                // Shutdown existing connection
                connectedEvent.Reset();

                // Alert connection users
                ConnectionLost?.Invoke(this, EventArgs.Empty);

                // Restore connection
                Task.Factory.StartNew(() => CreateConnection());
            }
        }

        private void OnConnectionRestored()
        {
            if (!connectedEvent.IsSet)
            {
                logger.Debug(ConnectionRestoredLogEntry);
                connectedEvent.Set();
                ConnectionRestored?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ConnectionCancelled()
        {
            this.ConnectionShuttingDown = true;
            logger.Debug(ConnectionCancelledLogEntry);
            connection.Close();
        }
        #endregion

    }
}
