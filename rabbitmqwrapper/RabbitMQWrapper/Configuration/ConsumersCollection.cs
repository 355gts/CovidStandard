using RabbitMQWrapper.Interfaces;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace RabbitMQWrapper.Configuration
{
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    sealed class ConsumersCollection : ConfigurationElementCollection, IConsumerConfigurations
    {
        protected override ConfigurationElement CreateNewElement()
            => new ConsumerElement();

        protected override object GetElementKey(ConfigurationElement element)
            => ((ConsumerElement)element).Name;

        new public IConsumerConfiguration this[string name]
            => (ConsumerElement)BaseGet(name);

        public void Add(ConsumerElement consumer)
            => BaseAdd(consumer);
    }
}
