using RabbitMQWrapper.Interfaces;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQWrapper.Configuration
{
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    sealed class PublishersCollection : ConfigurationElementCollection, IPublisherConfigurations
    {
        protected override ConfigurationElement CreateNewElement()
            => new PublisherElement();

        protected override object GetElementKey(ConfigurationElement element)
            => ((PublisherElement)element).Name;

        new public IPublisherConfiguration this[string name]
            => (PublisherElement)BaseGet(name);

        public void Add(PublisherElement consumer)
            => BaseAdd(consumer);
    }
}
