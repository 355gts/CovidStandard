using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQWrapper.Aggregator;
using RabbitMQWrapper.Enumerations;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper.UnitTest.Aggregator
{
    [TestClass]
    public class AggregateEventListener
    {
        private Mock<IQueueConsumer<TestMessage>> mockQueueConsumer;
        private MockAggegator aggregator;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        [TestInitialize]
        public void Setup()
        {
            mockQueueConsumer = new Mock<IQueueConsumer<TestMessage>>();
            aggregator = new MockAggegator(mockQueueConsumer.Object);

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        [TestMethod]
        public void Aggregator_Behaviour_NeverAcknowledge()
        {
            Assert.AreEqual(AcknowledgeBehaviour.Never, aggregator.Behavior());
        }

        [TestMethod]
        public void Aggregator_ProcessMessagesInGroup()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            Thread.Sleep(150);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(1L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(2L), Times.Once());
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(1, aggregator.ProcessAggregationCallCount);

            Assert.AreEqual(2, aggregator.ReceivedMessages[1].Count());
            Assert.AreEqual(testMessage1.Text, aggregator.ReceivedMessages[1].First().Text);
            Assert.AreEqual(testMessage2.Text, aggregator.ReceivedMessages[1].ElementAt(1).Text);
        }

        [TestMethod]
        public void Aggregator_ProcessFails_NegativelyAcknowledgesQueue()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };
            aggregator.ProcessMessagesSuccess = false;

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(1L), Times.Once());
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(2L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(1, aggregator.ProcessAggregationCallCount);

            Assert.AreEqual(2, aggregator.ReceivedMessages[1].Count());
            Assert.AreEqual(testMessage1.Text, aggregator.ReceivedMessages[1].First().Text);
            Assert.AreEqual(testMessage2.Text, aggregator.ReceivedMessages[1].ElementAt(1).Text);
        }

        [TestMethod]
        public void Aggregator_CancelledToken_Nothing()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };

            // test
            cancellationTokenSource.Cancel();
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(0, aggregator.ProcessAggregationCallCount);
        }

        [TestMethod]
        public void Aggregator_CancelledToken_Nothing_2()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(150);
            cancellationTokenSource.Cancel();

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(0, aggregator.ProcessAggregationCallCount);
        }

        [TestMethod]
        public void Aggregator_CannotGetGroup_Acknowledge()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };
            aggregator.GetGroupSuccess = false;

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(It.IsAny<ulong>()), Times.Exactly(2));

            Assert.AreEqual(0, aggregator.ProcessAggregationCallCount);
        }

        [TestMethod]
        public void Aggregator_DifferentGroups_DifferentAggregations()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 2L, Text = "message2" };

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            Thread.Sleep(150);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);

            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(1L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(2L), Times.Once());
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(2, aggregator.ProcessAggregationCallCount);

            Assert.AreEqual(1, aggregator.ReceivedMessages[1].Count());
            Assert.AreEqual(testMessage1.Text, aggregator.ReceivedMessages[1].First().Text);

            Assert.AreEqual(1, aggregator.ReceivedMessages[2].Count());
            Assert.AreEqual(testMessage2.Text, aggregator.ReceivedMessages[2].First().Text);
        }

        [TestMethod]
        public void Aggregator_MaxTimeoutReached_ProcessesData()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };
            TestMessage testMessage3 = new TestMessage { Group = 1L, Text = "message3" };
            TestMessage testMessage4 = new TestMessage { Group = 1L, Text = "message4" };
            TestMessage testMessage5 = new TestMessage { Group = 1L, Text = "message5" };

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage3, 3L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage4, 4L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage5, 5L, cancellationToken);
            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(1L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(2L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(3L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(4L), Times.Once());
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(2, aggregator.ProcessAggregationCallCount);

            Assert.AreEqual(4, aggregator.ReceivedMessages[1].Count());
            Assert.AreEqual(testMessage1.Text, aggregator.ReceivedMessages[1].First().Text);
            Assert.AreEqual(testMessage2.Text, aggregator.ReceivedMessages[1].ElementAt(1).Text);
            Assert.AreEqual(testMessage3.Text, aggregator.ReceivedMessages[1].ElementAt(2).Text);
            Assert.AreEqual(testMessage4.Text, aggregator.ReceivedMessages[1].ElementAt(3).Text);

            Assert.AreEqual(1, aggregator.ReceivedMessages[2].Count());
            Assert.AreEqual(testMessage5.Text, aggregator.ReceivedMessages[2].First().Text);
        }

        [TestMethod]
        public void Aggregator_ZeroMaxTimeout_ProcessedAsOneBlock()
        {
            // setup test data
            TestMessage testMessage1 = new TestMessage { Group = 1L, Text = "message1" };
            TestMessage testMessage2 = new TestMessage { Group = 1L, Text = "message2" };
            TestMessage testMessage3 = new TestMessage { Group = 1L, Text = "message3" };
            TestMessage testMessage4 = new TestMessage { Group = 1L, Text = "message4" };
            TestMessage testMessage5 = new TestMessage { Group = 1L, Text = "message5" };

            var aggregator = new MockAggegatorZeroMaxTimeOut(mockQueueConsumer.Object);

            // test
            aggregator.ProcessMessage(testMessage1, 1L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage2, 2L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage3, 3L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage4, 4L, cancellationToken);
            Thread.Sleep(200);
            aggregator.ProcessMessage(testMessage5, 5L, cancellationToken);
            Thread.Sleep(750);

            // assert
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(1L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(2L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(3L), Times.Once());
            mockQueueConsumer.Verify(m => m.AcknowledgeMessage(4L), Times.Once());
            mockQueueConsumer.Verify(m => m.NegativelyAcknowledge(It.IsAny<ulong>()), Times.Never());

            Assert.AreEqual(1, aggregator.ProcessAggregationCallCount);

            Assert.AreEqual(5, aggregator.ReceivedMessages[1].Count());
            Assert.AreEqual(testMessage1.Text, aggregator.ReceivedMessages[1].First().Text);
            Assert.AreEqual(testMessage2.Text, aggregator.ReceivedMessages[1].ElementAt(1).Text);
            Assert.AreEqual(testMessage3.Text, aggregator.ReceivedMessages[1].ElementAt(2).Text);
            Assert.AreEqual(testMessage4.Text, aggregator.ReceivedMessages[1].ElementAt(3).Text);
            Assert.AreEqual(testMessage5.Text, aggregator.ReceivedMessages[1].ElementAt(4).Text);
        }
    }

    public class TestMessage
    {
        public string Text { get; set; }
        public long Group { get; set; }
    }

    class MockAggegatorZeroMaxTimeOut : AggregateEventListener<TestMessage, long>
    {
        public bool GetGroupSuccess { private get; set; }
        public bool ProcessMessagesSuccess { private get; set; }

        public IDictionary<int, IEnumerable<TestMessage>> ReceivedMessages { get; private set; }
        public IDictionary<int, long> ReceivedGroup { get; private set; }
        public int ProcessAggregationCallCount { get; private set; }


        public MockAggegatorZeroMaxTimeOut(IQueueConsumer<TestMessage> queueConsumer) : base(queueConsumer)
        {
            GetGroupSuccess = true;
            ProcessMessagesSuccess = true;

            ReceivedMessages = new Dictionary<int, IEnumerable<TestMessage>>();
            ReceivedGroup = new Dictionary<int, long>();
        }

        public AcknowledgeBehaviour Behavior()
        {
            return Behaviour;
        }

        public void ProcessMessage(TestMessage message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            ProcessMessageAsync(message, deliveryTag, cancellationToken).GetAwaiter().GetResult();
        }

        protected override TimeSpan TimeoutTimeSpan => TimeSpan.FromMilliseconds(250);

        protected override TimeSpan MaxTimeoutTimeSpan => TimeSpan.FromTicks(0);

        protected override Task<QueueGroup<long>> GetAggregationGroup(TestMessage message)
        {
            return Task.FromResult(new QueueGroup<long> { Success = GetGroupSuccess, Group = message.Group });
        }

        protected override Task<bool> TryProcessAggregationGroup(IEnumerable<TestMessage> messages, long group, CancellationToken cancellationToken)
        {
            ProcessAggregationCallCount++;

            ReceivedGroup.Add(ProcessAggregationCallCount, group);
            ReceivedMessages.Add(ProcessAggregationCallCount, messages);

            return Task.FromResult(ProcessMessagesSuccess);
        }
    }

    class MockAggegator : AggregateEventListener<TestMessage, long>
    {
        public bool GetGroupSuccess { private get; set; }
        public bool ProcessMessagesSuccess { private get; set; }

        public IDictionary<int, IEnumerable<TestMessage>> ReceivedMessages { get; private set; }
        public IDictionary<int, long> ReceivedGroup { get; private set; }
        public int ProcessAggregationCallCount { get; private set; }


        public MockAggegator(IQueueConsumer<TestMessage> queueConsumer) : base(queueConsumer)
        {
            GetGroupSuccess = true;
            ProcessMessagesSuccess = true;

            ReceivedMessages = new Dictionary<int, IEnumerable<TestMessage>>();
            ReceivedGroup = new Dictionary<int, long>();
        }

        public AcknowledgeBehaviour Behavior() => Behaviour;

        public void ProcessMessage(TestMessage message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            ProcessMessageAsync(message, deliveryTag, cancellationToken).GetAwaiter().GetResult();
        }

        protected override TimeSpan TimeoutTimeSpan => TimeSpan.FromMilliseconds(250);

        protected override TimeSpan MaxTimeoutTimeSpan => TimeSpan.FromMilliseconds(450);

        protected override Task<QueueGroup<long>> GetAggregationGroup(TestMessage message)
        {
            return Task.FromResult(new QueueGroup<long> { Success = GetGroupSuccess, Group = message.Group });
        }

        protected override Task<bool> TryProcessAggregationGroup(IEnumerable<TestMessage> messages, long group, CancellationToken cancellationToken)
        {
            ProcessAggregationCallCount++;

            ReceivedGroup.Add(ProcessAggregationCallCount, group);
            ReceivedMessages.Add(ProcessAggregationCallCount, messages);

            return Task.FromResult(ProcessMessagesSuccess);
        }
    }
}
