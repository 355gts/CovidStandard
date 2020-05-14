using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Config
{
    [DataContract]
    [Serializable]
    public class RestClientConfiguration : ConfigurationSection, IRestClientConfiguration
    {
        [ConfigurationProperty("services", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ServicesCollection), AddItemName = "service")]
        [JsonProperty("services")]
        private ServicesCollection Services => (ServicesCollection)this["services"];

        IServiceConfigurations IRestClientConfiguration.Services => Services;
    }
}
