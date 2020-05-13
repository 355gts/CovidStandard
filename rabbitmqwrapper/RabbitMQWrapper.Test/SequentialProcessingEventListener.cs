using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQWrapper.Interfaces;
using RabbitMQWrapper.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWrapper.UnitTest
{
    [TestClass]
    public class SequentialProcessingEventListener
    {
        class TestSequentialProcessingEventListener : SequentialProcessingEventListener<string>
        {
            readonly Dictionary<string, bool> processingDictionary = new Dictionary<string, bool>();
            readonly Random random = new Random();

            public TestSequentialProcessingEventListener(IQueueConsumer<string> queueConsumer)
                : base(queueConsumer)
            {

            }

            protected override async Task ProcessMessageAsync(string message, ulong deliveryTag, string routingKey, CancellationToken cancellationToken)
            {
                Debug.WriteLine($"{DateTime.UtcNow}: {routingKey} - Processing Message: {deliveryTag}.");
                if (processingDictionary.ContainsKey(routingKey))
                {
                    Assert.IsFalse(processingDictionary[routingKey]);
                }

                processingDictionary[routingKey] = true;

                await Task.Delay(random.Next(1500) + 100);

                Debug.WriteLine($"{DateTime.UtcNow}: {routingKey} - Processed Message: {deliveryTag}.");
                processingDictionary[routingKey] = false;
            }
        }

        class TestQueueConsumer : IQueueConsumer<string>
        {
            readonly Random random = new Random();
            ulong currentDeliveryTag = 0;

            public IConsumerConfiguration Configuration { get { return null; } }

            public void AcknowledgeMessage(ulong deliveryTag)
            {
                // Do Nothing
            }

            public void Dispose()
            {
                // Do Nothing
            }

            public void NegativelyAcknowledge(ulong deliveryTag)
            {
                // Do Nothing
            }

            public void NegativelyAcknowledgeAndRequeue(ulong deliveryTag)
            {
                // Do Nothing
            }

            public bool TryGetNextMessage(int timeoutMilliseconds, out QueueMessage<string> message)
            {
                message = null;
                if (currentDeliveryTag > 300)
                    return false;

                Thread.Sleep(random.Next(500));
                currentDeliveryTag++;

                string routingKey = (random.Next(10)) + ".Test";

                message = new QueueMessage<string>("test", currentDeliveryTag, routingKey, null);

                if (currentDeliveryTag == 300)
                    Debug.WriteLine("Finishing queue delivery");

                return true;
            }
        }

        [TestClass]
        public class WhenConstructed
        {
            [TestMethod]
            [ExpectedException(typeof(ArgumentNullException))]
            public void WithNullQueueConsumer_ThrowsArgumentNullException()
            {
                new TestSequentialProcessingEventListener(null);
            }
        }

        //[TestMethod]
        public void MyTestMethod()
        {
            for (int i = 0; i < 10; i++)
            {
                var eventListener = new TestSequentialProcessingEventListener(new TestQueueConsumer());

                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(30000);
                eventListener.Run(cts.Token);
            }
        }
    }
}
