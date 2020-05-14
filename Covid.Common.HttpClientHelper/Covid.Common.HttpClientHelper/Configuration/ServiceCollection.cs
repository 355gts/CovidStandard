using System.Configuration;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [ConfigurationCollection(typeof(Service))]
    public class ServiceCollection : ConfigurationElementCollection, IServiceCollection
    {
        protected override ConfigurationElement CreateNewElement()
            => new Service();

        protected override object GetElementKey(ConfigurationElement element)
            => ((Service)element).Name;

        new public IServiceConfiguration this[string name]
            => (Service)BaseGet(name);

        public void Add(Service consumer)
            => BaseAdd(consumer);

    }
}
