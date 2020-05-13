using CommonUtils.Certificates;
using CommonUtils.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQWrapper.Interfaces;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using QW = RabbitMQWrapper;

namespace RabbitMQWrapper.UnitTest
{
    [TestClass]
    public class RabbitMQConnection
    {
        [TestClass]
        public class CreateModel
        {
            private Mock<IConnection> connectionMock;
            private Mock<ConnectionFactory> connectionFactoryMock;
            private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
            private Mock<ICertificateHelper> certificateHelperMock;
            private CancellationTokenSource cancellationTokenSource;

            private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
            private const string clientCertificateLocation = @"\\sftshare\certs\cert.cer";
            private const string clientCertificatePassword = "ksdfh4349*(£Q";

            [TestInitialize]
            public void Setup()
            {
                this.connectionMock = new Mock<IConnection>();

                this.connectionFactoryMock = new Mock<ConnectionFactory>();
                this.connectionFactoryMock.Setup(m => m.CreateConnection())
                    .Returns(connectionMock.Object);

                this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                this.queueWrapperConfigMock.Setup(m => m.Uri)
                    .Returns(mqUri);
                this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                    .Returns(clientCertificateLocation);

                this.certificateHelperMock = new Mock<ICertificateHelper>();
                var certificate = new X509Certificate2Collection();
                certificateHelperMock
                    .Setup(m => m.TryFindCertificate(It.IsAny<string>(), out certificate))
                    .Returns(true);

                this.cancellationTokenSource = new CancellationTokenSource();
            }

            [TestMethod]
            public void Returns_IConnectionCreateModel()
            {
                var mockModel = new Mock<IModel>();

                this.connectionMock
                    .Setup(m => m.CreateModel())
                    .Returns(mockModel.Object);

                var rabbitMqConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                                                queueWrapperConfigMock.Object,
                                                certificateHelperMock.Object,
                                                cancellationTokenSource.Token);

                var model = rabbitMqConnection.CreateModel();

                this.connectionMock.Verify(m => m.CreateModel(), Times.Once());
                Assert.AreEqual(mockModel.Object, model);
            }
        }

        [TestClass]
        public class WhenConnectionShutdown
        {
            private Mock<IConnection> connectionMock;
            private Mock<ConnectionFactory> connectionFactoryMock;
            private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
            private Mock<ICertificateHelper> certificateHelperMock;
            private CancellationTokenSource cancellationTokenSource;
            private QW.ConnectionHandler rabbitMQConnection;

            private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
            private const string clientCertificateLocation = @"\\sftshare\certs\cert.cer";
            private const string clientCertificatePassword = "ksdfh4349*(£Q";

            [TestInitialize]
            public void Setup()
            {
                this.connectionMock = new Mock<IConnection>();

                this.connectionFactoryMock = new Mock<ConnectionFactory>();
                this.connectionFactoryMock.Setup(m => m.CreateConnection())
                    .Returns(connectionMock.Object);

                this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                this.queueWrapperConfigMock.Setup(m => m.Uri)
                    .Returns(mqUri);
                this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                    .Returns(clientCertificateLocation);

                this.certificateHelperMock = new Mock<ICertificateHelper>();
                var certificate = new X509Certificate2Collection();
                certificateHelperMock
                    .Setup(m => m.TryFindCertificate(It.IsAny<string>(), out certificate))
                    .Returns(true);

                this.cancellationTokenSource = new CancellationTokenSource();

                this.rabbitMQConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                                                queueWrapperConfigMock.Object,
                                                certificateHelperMock.Object,
                                                cancellationTokenSource.Token);
            }

            [TestMethod]
            public void ConnectionLostEventFired()
            {
                int connectionLostFiredCount = 0;
                this.rabbitMQConnection.ConnectionLost += (s, e) => connectionLostFiredCount++;

                this.connectionMock.Raise(c => c.ConnectionShutdown += null, 
                    new ShutdownEventArgs(ShutdownInitiator.Library, 0, ""));

                Assert.AreEqual(1, connectionLostFiredCount);
            }

            [TestMethod]
            public void CallsCreateConnection()
            {
                this.connectionMock.Raise(c => c.ConnectionShutdown += null,
                    new ShutdownEventArgs(ShutdownInitiator.Application, 0, ""));

                Thread.Sleep(50);

                // invoked twice - once on construction and once due to event
                this.connectionFactoryMock.Verify(m => m.CreateConnection(), Times.Exactly(2));
            }

            [TestMethod]
            public void ConnectionRestoredEventFired()
            {
                int connectionRestoredFiredCount = 0;
                this.rabbitMQConnection.ConnectionRestored += (s, e) => connectionRestoredFiredCount++;

                this.connectionMock.Raise(c => c.ConnectionShutdown += null,
                    new ShutdownEventArgs(ShutdownInitiator.Peer, 0, ""));

                Thread.Sleep(50);

                Assert.AreEqual(1, connectionRestoredFiredCount);
            }
        }

        [TestClass]
        public class WhenConstructed
        {
            private Mock<IConnection> connectionMock;
            private Mock<ConnectionFactory> connectionFactoryMock;
            private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
            private Mock<ICertificateHelper> certificateHelperMock;
            private CancellationToken cancellationToken;

            private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
            private const string clientCertificateSubjectName = @"\\sftshare\certs\cert.cer";

            [TestInitialize]
            public void Setup()
            {

                this.connectionMock = new Mock<IConnection>();

                this.connectionFactoryMock = new Mock<ConnectionFactory>();
                this.connectionFactoryMock.Setup(m => m.CreateConnection())
                    .Returns(this.connectionMock.Object);

                this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                this.queueWrapperConfigMock.Setup(m => m.Uri)
                    .Returns(mqUri);
                this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                    .Returns(clientCertificateSubjectName);

                this.certificateHelperMock = new Mock<ICertificateHelper>();
                var certificate = new X509Certificate2Collection();
                certificateHelperMock
                    .Setup(m => m.TryFindCertificate(clientCertificateSubjectName, out certificate))
                    .Returns(true);

                this.cancellationToken = new CancellationTokenSource().Token;
            }

            [TestMethod]
            public void ConfiguresConnectionFactory()
            {
                new QW.ConnectionHandler(connectionFactoryMock.Object,
                    queueWrapperConfigMock.Object,
                    certificateHelperMock.Object,
                    cancellationToken);

                Assert.AreEqual(mqUri.Host, connectionFactoryMock.Object.HostName);
                Assert.AreEqual(mqUri.Port, connectionFactoryMock.Object.Port);
                Assert.IsFalse(connectionFactoryMock.Object.AutomaticRecoveryEnabled);
                Assert.AreEqual(connectionFactoryMock.Object.RequestedHeartbeat, queueWrapperConfigMock.Object.RabbitMQHeartbeatSeconds);
                Assert.AreEqual(mqUri.Host, connectionFactoryMock.Object.Ssl.ServerName);
                Assert.IsTrue(connectionFactoryMock.Object.Ssl.Enabled);
                Assert.AreEqual(System.Security.Authentication.SslProtocols.Tls12, connectionFactoryMock.Object.Ssl.Version);
                Assert.AreEqual(1, connectionFactoryMock.Object.AuthMechanisms.Count);
                Assert.IsInstanceOfType(connectionFactoryMock.Object.AuthMechanisms[0], typeof(ExternalMechanismFactory));
            }

            [TestMethod]
            public void CallsFindCertificate()
            {
                new QW.ConnectionHandler(connectionFactoryMock.Object,
                    queueWrapperConfigMock.Object,
                    certificateHelperMock.Object,
                    cancellationToken);

                var certificate = new X509Certificate2Collection();
                this.certificateHelperMock.Verify(m => m.TryFindCertificate(clientCertificateSubjectName, out certificate), Times.Once());
            }

            [TestMethod]
            public void CallsCreateConnection()
            {
                new QW.ConnectionHandler(connectionFactoryMock.Object,
                    queueWrapperConfigMock.Object,
                    certificateHelperMock.Object,
                    cancellationToken);

                connectionFactoryMock.Verify(m => m.CreateConnection(), Times.Once());
            }

            [TestMethod]
            public void ConnectionShuttingDown_IsFalse()
            {
                var rabbitMQConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                    queueWrapperConfigMock.Object,
                    certificateHelperMock.Object,
                    cancellationToken);

                Assert.IsFalse(rabbitMQConnection.ConnectionShuttingDown);
            }

            [TestClass]
            public class WithMissingCertificate
            {
                private Mock<IConnection> connectionMock;
                private Mock<ConnectionFactory> connectionFactoryMock;
                private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
                private Mock<ICertificateHelper> certificateHelperMock;
                private CancellationToken cancellationToken;

                private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
                private const string clientCertificateSubjectName = @"\\sftshare\certs\cert.cer";

                [TestInitialize]
                public void Setup()
                {

                    this.connectionMock = new Mock<IConnection>();

                    this.connectionFactoryMock = new Mock<ConnectionFactory>();
                    this.connectionFactoryMock.Setup(m => m.CreateConnection())
                        .Returns(this.connectionMock.Object);

                    this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                    this.queueWrapperConfigMock.Setup(m => m.Uri)
                        .Returns(mqUri);
                    this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                        .Returns(clientCertificateSubjectName);

                    this.certificateHelperMock = new Mock<ICertificateHelper>();

                    this.cancellationToken = new CancellationTokenSource().Token;
                }

                [TestMethod]
                [ExpectedException(typeof(FatalErrorException))]
                public void ThrowsFatalErrorException()
                {
                    var certificate = new X509Certificate2Collection();
                    certificateHelperMock
                        .Setup(m => m.TryFindCertificate(clientCertificateSubjectName, out certificate))
                        .Returns(false);

                    new QW.ConnectionHandler(connectionFactoryMock.Object,
                    queueWrapperConfigMock.Object,
                    certificateHelperMock.Object,
                    cancellationToken);
                }
            }

            [TestClass]
            public class WithNullCertificateHelper
            {
                [TestMethod]
                [ExpectedException(typeof(ArgumentNullException))]
                public void ThrowsNullArgumentException()
                {
                    new QW.ConnectionHandler(new Mock<ConnectionFactory>().Object,
                        new Mock<IQueueWrapperConfiguration>().Object,
                        null,
                        new CancellationTokenSource().Token);
                }
            }

            [TestClass]
            public class WithNullQueueWrapperConfiguration
            {
                [TestMethod]
                [ExpectedException(typeof(ArgumentNullException))]
                public void ThrowsNullArgumentException()
                {
                    new QW.ConnectionHandler(new Mock<ConnectionFactory>().Object,
                        null,
                        new Mock<ICertificateHelper>().Object,
                        new CancellationTokenSource().Token);
                }
            }

            [TestClass]
            public class WithNullConnectionFactory
            {
                [TestMethod]
                [ExpectedException(typeof(ArgumentNullException))]
                public void ThrowsNullArgumentException()
                {
                    new QW.ConnectionHandler(null,
                        new Mock<IQueueWrapperConfiguration>().Object,
                        new Mock<ICertificateHelper>().Object,
                        new CancellationTokenSource().Token);
                }
            }
        }

        [TestClass]
        public class WhenCancelled
        {
            private Mock<IConnection> connectionMock;
            private Mock<ConnectionFactory> connectionFactoryMock;
            private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
            private Mock<ICertificateHelper> certificateHelperMock;
            private CancellationTokenSource cancellationTokenSource;

            private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
            private const string clientCertificateLocation = @"\\sftshare\certs\cert.cer";
            private const string clientCertificatePassword = "ksdfh4349*(£Q";

            [TestInitialize]
            public void Setup()
            {
                this.connectionMock = new Mock<IConnection>();

                this.connectionFactoryMock = new Mock<ConnectionFactory>();
                this.connectionFactoryMock.Setup(m => m.CreateConnection())
                    .Returns(connectionMock.Object);

                this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                this.queueWrapperConfigMock.Setup(m => m.Uri)
                    .Returns(mqUri);
                this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                    .Returns(clientCertificateLocation);

                this.certificateHelperMock = new Mock<ICertificateHelper>();
                var certificate = new X509Certificate2Collection();
                certificateHelperMock
                    .Setup(m => m.TryFindCertificate(It.IsAny<string>(), out certificate))
                    .Returns(true);

                this.cancellationTokenSource = new CancellationTokenSource();
            }

            [TestMethod]
            public void ClosesConnection()
            {
                var rabbitMqConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                                                queueWrapperConfigMock.Object,
                                                certificateHelperMock.Object,
                                                cancellationTokenSource.Token);

                cancellationTokenSource.Cancel();

                this.connectionMock.Verify(m => m.Close(), Times.Once());
            }

            [TestMethod]
            public void ConnectionShuttingDown_IsTrue()
            {
                var rabbitMqConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                                                queueWrapperConfigMock.Object,
                                                certificateHelperMock.Object,
                                                cancellationTokenSource.Token);

                cancellationTokenSource.Cancel();

                Assert.IsTrue(rabbitMqConnection.ConnectionShuttingDown);
            }
        }

        [TestClass]
        public class WhenDisposed
        {
            private Mock<IConnection> connectionMock;
            private Mock<ConnectionFactory> connectionFactoryMock;
            private Mock<IQueueWrapperConfiguration> queueWrapperConfigMock;
            private Mock<ICertificateHelper> certificateHelperMock;
            private CancellationToken cancellationToken;

            private readonly Uri mqUri = new Uri(@"amqps://sftmq.test.shell:12346/");
            private const string clientCertificateLocation = @"\\sftshare\certs\cert.cer";
            private const string clientCertificatePassword = "ksdfh4349*(£Q";

            [TestInitialize]
            public void Setup()
            {
                this.connectionMock = new Mock<IConnection>();

                this.connectionFactoryMock = new Mock<ConnectionFactory>();
                this.connectionFactoryMock.Setup(m => m.CreateConnection())
                    .Returns(connectionMock.Object);

                this.queueWrapperConfigMock = new Mock<IQueueWrapperConfiguration>();
                this.queueWrapperConfigMock.Setup(m => m.Uri)
                    .Returns(mqUri);
                this.queueWrapperConfigMock.Setup(m => m.ClientCertificateSubjectName)
                    .Returns(clientCertificateLocation);

                this.certificateHelperMock = new Mock<ICertificateHelper>();
                var certificate = new X509Certificate2Collection();
                certificateHelperMock
                    .Setup(m => m.TryFindCertificate(It.IsAny<string>(), out certificate))
                    .Returns(true);

                this.cancellationToken = new CancellationTokenSource().Token;
            }

            [TestMethod]
            public void DisposesConnection()
            {
                var rabbitMqConnection = new QW.ConnectionHandler(connectionFactoryMock.Object,
                                                queueWrapperConfigMock.Object,
                                                certificateHelperMock.Object,
                                                cancellationToken);
                rabbitMqConnection.Dispose();

                this.connectionMock.Verify(m => m.Dispose(), Times.Once());
            }
        }
    }
}
