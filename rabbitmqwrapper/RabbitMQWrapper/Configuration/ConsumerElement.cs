using RabbitMQWrapper.Interfaces;
using System.Configuration;

namespace RabbitMQWrapper.Configuration
{
    sealed class ConsumerElement : ConfigurationElement, IConsumerConfiguration
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("queueName")]
        public string QueueName => (string)this["queueName"];

        [ConfigurationProperty("exchangeName")]
        public string ExchangeName => (string)this["exchangeName"];

        [ConfigurationProperty("routingKey")]
        public string RoutingKey => (string)this["routingKey"];

        [ConfigurationProperty("messageWaitTimeoutMilliseconds", DefaultValue = 1000, IsRequired = false)]
        public int MessageWaitTimeoutMilliseconds => (int)this["messageWaitTimeoutMilliseconds"];
    }
}
