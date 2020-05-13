using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQWrapper.Interfaces;
using QW = RabbitMQWrapper.Configuration;

namespace RabbitMQWrapper.UnitTest
{
    [TestClass]
    public class QueueWrapperConfiguration
    {
        [TestMethod]
        public void LoadsUriCorrectly()
        {
            var config = QW.QueueWrapperConfiguration.GetSection();

            Assert.AreEqual("amqps://localhost:12456/", config.Uri.AbsoluteUri);
            Assert.AreEqual("localhost", config.Uri.Host);
        }

        [TestMethod]
        public void LoadsClientCertificateLocationCorrectly()
        {
            var config = QW.QueueWrapperConfiguration.GetSection();

            Assert.AreEqual("dummycertificate", config.ClientCertificateSubjectName);
        }

        [TestMethod]
        public void LoadsMessagePrefetchCountCorrectly()
        {
            var config = QW.QueueWrapperConfiguration.GetSection();

            Assert.AreEqual(13, config.MessagePrefetchCount);
        }

        [TestMethod]
        public void LoadsConsumersCorrectly()
        {
            IQueueWrapperConfiguration config = QW.QueueWrapperConfiguration.GetSection();

            var consumer1 = config.Consumers["consumer1"];
            Assert.AreEqual("rabbit_q", consumer1.QueueName);
            Assert.AreEqual("", consumer1.ExchangeName);
            Assert.AreEqual("", consumer1.RoutingKey);

            var consumer2 = config.Consumers["consumer2"];
            Assert.AreEqual("", consumer2.QueueName);
            Assert.AreEqual("rabbit_ex2", consumer2.ExchangeName);
            Assert.AreEqual("rabbit_routing2", consumer2.RoutingKey);
        }

        [TestMethod]
        public void ConsumerDoesntExist_ReturnsNull()
        {
            IQueueWrapperConfiguration config = QW.QueueWrapperConfiguration.GetSection();

            var consumer = config.Consumers["iDontExist"];
            Assert.IsNull(consumer);
        }

        [TestMethod]
        public void LoadsPublishersCorrectly()
        {
            IQueueWrapperConfiguration config = QW.QueueWrapperConfiguration.GetSection();

            var publisher1 = config.Publishers["publisher1"];
            Assert.AreEqual("rabbit_ex1", publisher1.ExchangeName);
            Assert.AreEqual("rabbit_routing1", publisher1.RoutingKey);
            Assert.IsTrue(publisher1.PublishesPersistentMessages);

            var publisher2 = config.Publishers["publisher2"];
            Assert.AreEqual("rabbit_ex2", publisher2.ExchangeName);
            Assert.AreEqual("rabbit_routing2", publisher2.RoutingKey);
            Assert.IsFalse(publisher2.PublishesPersistentMessages);
        }

        [TestMethod]
        public void PublisherDoesntExist_ReturnsNull()
        {
            IQueueWrapperConfiguration config = QW.QueueWrapperConfiguration.GetSection();

            var publisher = config.Publishers["iDontExist"];
            Assert.IsNull(publisher);
        }
    }
}
