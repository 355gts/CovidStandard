using RabbitMQWrapper.Interfaces;
using System.Configuration;

namespace RabbitMQWrapper.Configuration
{
    sealed class PublisherElement : ConfigurationElement, IPublisherConfiguration
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("exchangeName", IsRequired = true)]
        public string ExchangeName => (string)this["exchangeName"];

        [ConfigurationProperty("routingKey", IsRequired = true)]
        public string RoutingKey => (string)this["routingKey"];

        [ConfigurationProperty("publishesPersistentMessages", IsRequired = false, DefaultValue = true)]
        public bool PublishesPersistentMessages => (bool)this["publishesPersistentMessages"];
    }
}
