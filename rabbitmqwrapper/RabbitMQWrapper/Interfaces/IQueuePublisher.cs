using System;
using System.Collections.Generic;

namespace RabbitMQWrapper.Interfaces
{
    public interface IQueuePublisher<T> : IDisposable where T : class
    {
        /// <summary>
        /// Publishes a message to the exchange.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        void Publish(T message);

        /// <summary>
        /// Publishes a message to the exchange, using the specified routing key 
        /// instead of the routing key in the configuration file.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="dynamicRoutingKey">The routing key to use.</param>
        void Publish(T message, string dynamicRoutingKey);
        
        /// <summary>
        /// Publishes a message to the exchange, including the specified custom headers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="headers">The additional header items to add to the message.</param>
        void Publish(T message, IDictionary<string, object> headers);

        /// <summary>
        /// Publishes a message to the exchange, using the specified routing key 
        /// instead of the routing key in the configuration file.
        /// Includes the specified custom headers on the message.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="headers">The additional header items to add to the message.</param>
        /// <param name="dynamicRoutingKey">The routing key to use.</param>
        void Publish(T message, IDictionary<string, object> headers, string dynamicRoutingKey);
    }
}
