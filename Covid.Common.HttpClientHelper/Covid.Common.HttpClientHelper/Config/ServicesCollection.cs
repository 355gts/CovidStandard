using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Config
{
    [DataContract]
    [Serializable]
    public class ServicesCollection : ConfigurationElementCollection, IServiceConfigurations
    {
        protected override ConfigurationElement CreateNewElement()
            => new ServiceElement();

        protected override object GetElementKey(ConfigurationElement element)
            => ((ServiceElement)element).Name;

        new public IServiceConfiguration this[string name]
            => (ServiceElement)BaseGet(name);

        public void Add(ServiceElement consumer)
            => BaseAdd(consumer);
    }
}
