using RabbitMQWrapper.Model;
using System;

namespace RabbitMQWrapper.Interfaces
{
    public interface IQueueConsumer<T> : IDisposable where T : class
    {
        /// <summary>
        /// Property to provide access to the consumers configuration.
        /// </summary>
        IConsumerConfiguration Configuration { get; }

        /// <summary>
        /// Blocking method to retrieve the next available message from the queue, with timeout.
        /// </summary>
        /// <param name="timeoutMilliseconds">The number of milliseconds to block for before timing out.</param>
        /// <param name="message">The next available message from the queue.</param>
        /// <returns>true if a message was returned, false otherwise.</returns>
        bool TryGetNextMessage(int timeoutMilliseconds, out QueueMessage<T> message);

        /// <summary>
        /// Acknowledges the message identified by the <paramref name="deliveryTag"/>.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to acknowledge.</param>
        void AcknowledgeMessage(ulong deliveryTag);

        /// <summary>
        /// Negatively acknowledges and requeues the message identified by the <paramref name="deliveryTag"/>.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to requeue.</param>
        void NegativelyAcknowledgeAndRequeue(ulong deliveryTag);

        /// <summary>
        /// Negatively acknowledges the message identified by the <paramref name="deliveryTag"/>.
        /// </summary>
        /// <param name="deliveryTag">The delivery tag of the message to negatively acnowledge.</param>
        void NegativelyAcknowledge(ulong deliveryTag);
    }
}
