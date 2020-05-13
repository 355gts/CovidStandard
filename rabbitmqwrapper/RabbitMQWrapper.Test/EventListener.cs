using CommonUtils.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using RabbitMQWrapper.Enumerations;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper.UnitTest
{
    [TestClass]
    public class EventListener
    {
        [TestClass]
        public class WhenConstructedWithNullQueueConsumer
        {
            [TestMethod]
            public void ThrowsNullArgumentException()
            {
                ThrowsNullArgumentException<GenericParameterHelper>();
            }

            private void ThrowsNullArgumentException<T>() where T : class
            {
                bool exceptionCaught = false;

                try
                {
                    var eventListener = new Mock<EventListener<T>>(new object[] { null }).Object;
                }
                catch (TargetInvocationException e)
                {
                    exceptionCaught = true;
                    Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                }

                Assert.IsTrue(exceptionCaught, "Expected Argument Null exception, but no exception was thrown.");
            }
        }

        [TestMethod]
        public void DefaultAcknolwedgementBehaviour_IsAcknowledgeAfterProcessing()
        {
            DefaultAcknolwedgementBehaviour_IsAcknowledgeAfterProcessing<GenericParameterHelper>();
        }

        private void DefaultAcknolwedgementBehaviour_IsAcknowledgeAfterProcessing<T>() where T : class
        {
            var queueConsumer = new Mock<IQueueConsumer<T>>().Object;
            var eventListener = new Mock<EventListener<T>>(queueConsumer).Object;
            Assert.AreEqual(AcknowledgeBehaviour.AfterProcess,
                new PrivateObject(eventListener).GetFieldOrProperty("Behaviour"));
        }

        [TestMethod]
        public void CancellationTokenCancelled_NoAttemptToReceiveMessage()
        {
            CancellationTokenCancelled_NoAttemptToReceiveMessage<GenericParameterHelper>();
        }

        private void CancellationTokenCancelled_NoAttemptToReceiveMessage<T>() where T : class
        {
            var queueConsumerMock = new Mock<IQueueConsumer<T>>();
            var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);
            eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                .Returns(AcknowledgeBehaviour.AfterProcess);

            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            eventListenerMock.Object.Run(cancellationToken.Token);

            QueueMessage<T> message;
            queueConsumerMock.Verify(m => m.TryGetNextMessage(It.IsAny<int>(), out message), Times.Never());

        }

        [TestClass]
        public class WhenRunningWithAfterProcessAcknowledgementBehaviour
        {
            [TestMethod]
            public void ProcessMessageSuccess_AcknowledgesMessage()
            {
                ProcessMessageSuccess_AcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageSuccess_AcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);
                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.AfterProcess);

                QueueMessage<T> message = new QueueMessage<T>(new T(), 100, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(100), Times.Once());
                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageFatalError_NegativelyAcknowledgesAndRequeuesMessage()
            {
                ProcessMessageFatalError_NegativelyAcknowledgesAndRequeuesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageFatalError_NegativelyAcknowledgesAndRequeuesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.AfterProcess);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new FatalErrorException());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 103, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token))
                    .ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                Thread.Sleep(500);

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(103), Times.Once());
            }

            [TestMethod]
            public void ProcessMessageProcessingError_NegativelyAcknowledgesMessage()
            {
                ProcessMessageProcessingError_NegativelyAcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageProcessingError_NegativelyAcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.AfterProcess);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new Exception());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 102, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(102), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }
        }

        [TestClass]
        public class WhenRunningWithBeforeProcessAcknowledgementBehaviour
        {
            [TestMethod]
            public void ProcessMessageSuccess_AcknowledgesMessage()
            {
                ProcessMessageSuccess_AcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageSuccess_AcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);
                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.BeforeProcess);

                QueueMessage<T> message = new QueueMessage<T>(new T(), 100, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(100), Times.Once());
                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageFatalError_AcknowledgesMessage()
            {
                ProcessMessageFatalError_AcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageFatalError_AcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.BeforeProcess);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new FatalErrorException());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 103, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token))
                    .ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                Thread.Sleep(500);

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.AcknowledgeMessage(103), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageProcessingError_AcknowledgesMessage()
            {
                ProcessMessageProcessingError_AcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageProcessingError_AcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.BeforeProcess);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new Exception());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 102, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.AcknowledgeMessage(102), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }
        }

        [TestClass]
        public class WhenRunningWithNeverAcknowledgementBehaviour
        {
            [TestMethod]
            public void ProcessMessageSuccess_DoesNotAcknowledgeMessage()
            {
                ProcessMessageSuccess_DoesNotAcknowledgeMessage<GenericParameterHelper>();
            }

            private void ProcessMessageSuccess_DoesNotAcknowledgeMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);
                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Never);

                QueueMessage<T> message = new QueueMessage<T>(new T(), 100, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageFatalError_DoesNotAcknowledgeMessage()
            {
                ProcessMessageFatalError_DoesNotAcknowledgeMessage<GenericParameterHelper>();
            }

            private void ProcessMessageFatalError_DoesNotAcknowledgeMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Never);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new FatalErrorException());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 103, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token))
                    .ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                Thread.Sleep(500);

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageProcessingError_DoesNotAcknowledgeMessage()
            {
                ProcessMessageProcessingError_DoesNotAcknowledgeMessage<GenericParameterHelper>();
            }

            private void ProcessMessageProcessingError_DoesNotAcknowledgeMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Never);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new Exception());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 102, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }
        }

        [TestClass]
        public class WhenRunningWithAsyncAcknowledgementBehaviour
        {
            public event EventHandler<AcknowledgeEventArgs> AcknowledgeMessageEvent;

            private void OnAcknowledgeMessage(AcknowledgeEventArgs eventArgs)
            {
                if (AcknowledgeMessageEvent != null)
                    AcknowledgeMessageEvent?.Invoke(this, eventArgs);
            }

            [TestMethod]
            public void ProcessMessageAsyncSuccess()
            {
                ProcessMessageAsyncSuccess<GenericParameterHelper>();
            }

            private void ProcessMessageAsyncSuccess<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);
                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Async);

                QueueMessage<T> message = new QueueMessage<T>(new T(), 100, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                AcknowledgeMessageEvent += eventListenerMock.Object.OnAcknowledgeMessage;
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                OnAcknowledgeMessage(new AcknowledgeEventArgs() { DeliveryTags = new ConcurrentBag<ulong>() { 100 } });

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(100), Times.Once());
                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }

            [TestMethod]
            public void ProcessMessageAsyncFatalError_NegativelyAcknowledgesAndRequeuesMessage()
            {
                ProcessMessageAsyncFatalError_NegativelyAcknowledgesAndRequeuesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageAsyncFatalError_NegativelyAcknowledgesAndRequeuesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Async);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new FatalErrorException());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 103, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token))
                    .ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);

                Thread.Sleep(500);

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(103), Times.Once());
            }

            [TestMethod]
            public void ProcessMessageAsyncProcessingError_NegativelyAcknowledgesMessage()
            {
                ProcessMessageAsyncProcessingError_NegativelyAcknowledgesMessage<GenericParameterHelper>();
            }

            private void ProcessMessageAsyncProcessingError_NegativelyAcknowledgesMessage<T>() where T : class, new()
            {
                var queueConsumerMock = new Mock<IQueueConsumer<T>>();
                var consumerConfigurationMock = new Mock<IConsumerConfiguration>();
                queueConsumerMock.SetupGet(c => c.Configuration).Returns(consumerConfigurationMock.Object);
                consumerConfigurationMock.SetupGet(c => c.MessageWaitTimeoutMilliseconds).Returns(1000);

                var eventListenerMock = new Mock<EventListener<T>>(queueConsumerMock.Object);

                eventListenerMock.Protected().Setup<AcknowledgeBehaviour>("Behaviour")
                    .Returns(AcknowledgeBehaviour.Async);

                eventListenerMock.Protected().Setup("ProcessMessageAsync",
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), ItExpr.IsAny<CancellationToken>())
                    .Throws(new Exception());

                QueueMessage<T> message = new QueueMessage<T>(new T(), 102, "routingKey", null);
                queueConsumerMock.SetupSequence(m => m.TryGetNextMessage(It.IsAny<int>(), out message))
                    .Returns(true).Returns(false);

                var cancellationToken = new CancellationTokenSource();
                Task.Factory.StartNew(() => eventListenerMock.Object.Run(cancellationToken.Token));

                Thread.Sleep(500);
                cancellationToken.Cancel();

                eventListenerMock.Protected().Verify("ProcessMessageAsync", Times.Once(),
                    ItExpr.IsAny<T>(), ItExpr.IsAny<ulong>(), cancellationToken.Token);

                queueConsumerMock.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledge(102), Times.Once());
                queueConsumerMock.Verify(m => m.NegativelyAcknowledgeAndRequeue(It.IsAny<ulong>()), Times.Never());
            }
        }
    }
}
