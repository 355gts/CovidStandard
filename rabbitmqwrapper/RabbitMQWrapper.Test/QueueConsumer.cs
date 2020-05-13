using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UUT = RabbitMQWrapper;
using Moq;
using CommonUtils.Validation;
using RabbitMQWrapper.Interfaces;
using RabbitMQ.Client;

namespace RabbitMQWrapper.Test
{
    [TestClass]
    public class QueueConsumer
    {
        private Mock<IConnectionHandler> mockConnectionHandler;
        private Mock<IQueueWrapperConfiguration> mockQueueWrapperConfiguration;
        private Mock<IConsumerConfigurations> mockConsumerConfigurations;
        private Mock<IConsumerConfiguration> mockConsumerConfiguration;
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<IModel> mockModel;


        private UUT.QueueConsumer<TestObject> queueConsumer;

        private const string queueName = "queueName";
        private const ushort messagePrefetchCount = 12;
        private const ulong deliveryTag = 23;

        [TestInitialize]
        public void SetUp()
        {
            mockConnectionHandler = new Mock<IConnectionHandler>();
            mockQueueWrapperConfiguration = new Mock<IQueueWrapperConfiguration>();
            mockConsumerConfigurations = new Mock<IConsumerConfigurations>();
            mockConsumerConfiguration = new Mock<IConsumerConfiguration>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockModel = new Mock<IModel>();

            mockQueueWrapperConfiguration.Setup(m => m.Consumers).Returns(mockConsumerConfigurations.Object);
            mockQueueWrapperConfiguration.Setup(m => m.MessagePrefetchCount).Returns(messagePrefetchCount);
            mockConsumerConfigurations.Setup(m => m[queueName]).Returns(mockConsumerConfiguration.Object);

            mockConnectionHandler.Setup(m => m.CreateModel()).Returns(mockModel.Object);
            mockConsumerConfiguration.Setup(m => m.QueueName).Returns(queueName);


            queueConsumer = new QueueConsumer<TestObject>(queueName, 
                mockConnectionHandler.Object, 
                mockQueueWrapperConfiguration.Object, 
                mockValidationHelper.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullQueueName_ArgumentNullException()
        {
            new QueueConsumer<TestObject>(null,
                   mockConnectionHandler.Object,
                   mockQueueWrapperConfiguration.Object,
                   mockValidationHelper.Object);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullConnectionHandler_ArgumentNullException()
        {
            new QueueConsumer<TestObject>(queueName,
                   null,
                   mockQueueWrapperConfiguration.Object,
                   mockValidationHelper.Object);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullQueueWrapperConfiguration_ArgumentNullException()
        {
            new QueueConsumer<TestObject>(queueName,
                   mockConnectionHandler.Object,
                   null,
                   mockValidationHelper.Object);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullValidationHelper_ArgumentNullException()
        {
            new QueueConsumer<TestObject>(queueName,
                   mockConnectionHandler.Object,
                   mockQueueWrapperConfiguration.Object,
                   null);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_NoConfigForQueue_ArgumentException()
        {
            mockConsumerConfigurations.Setup(m => m[queueName]).Returns((IConsumerConfiguration) null);

            new QueueConsumer<TestObject>(queueName,
                   mockConnectionHandler.Object,
                   mockQueueWrapperConfiguration.Object,
                   mockValidationHelper.Object);
        }

        [TestMethod]
        public void NegativelyAcknowledgeAndRequeue_NoConnection()
        {
            mockConnectionHandler.Raise(m => m.ConnectionLost += null, EventArgs.Empty);

            queueConsumer.NegativelyAcknowledgeAndRequeue(deliveryTag);

            mockModel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [TestMethod]
        public void NegativelyAcknowledgeAndRequeue_Success()
        {
            queueConsumer.NegativelyAcknowledgeAndRequeue(deliveryTag);

            mockModel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            mockModel.Verify(m => m.BasicNack(deliveryTag, false, true), Times.Once);
        }

        [TestMethod]
        public void NegativelyAcknowledge_NoConnection()
        {
            mockConnectionHandler.Raise(m => m.ConnectionLost += null, EventArgs.Empty);

            queueConsumer.NegativelyAcknowledge(deliveryTag);

            mockModel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());
        }

        [TestMethod]
        public void NegativelyAcknowledge_SwallowsException()
        {
            mockModel.Setup(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>())).Throws(new FieldAccessException());

            queueConsumer.NegativelyAcknowledge(deliveryTag);

            mockModel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            mockModel.Verify(m => m.BasicNack(deliveryTag, false, false), Times.Once);
        }

        [TestMethod]
        public void NegativelyAcknowledge_Success()
        {
            queueConsumer.NegativelyAcknowledge(deliveryTag);

            mockModel.Verify(m => m.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            mockModel.Verify(m => m.BasicNack(deliveryTag, false, false), Times.Once);
        }

        [TestMethod]
        public void AcknowledgeMessage_NoConnection()
        {
            mockConnectionHandler.Raise(m => m.ConnectionLost += null, EventArgs.Empty);

            queueConsumer.AcknowledgeMessage(deliveryTag);

            mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never());
        }

        [TestMethod]
        public void AcknowledgeMessage_Success()
        {
            queueConsumer.AcknowledgeMessage(deliveryTag);

            mockModel.Verify(m => m.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Once);
            mockModel.Verify(m => m.BasicAck(deliveryTag, false), Times.Once);
        }
    }

    class TestObject
    {
        public string id { get; set; }
    }
}
