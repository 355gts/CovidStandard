using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [DataContract]
    [Serializable]
    public class RestClientConfiguration : ConfigurationSection, IRestClientConfiguration
    {
        [ConfigurationProperty("services", IsRequired = false, IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(ServiceCollection), AddItemName = "add")]
        [JsonProperty("services")]
        private ServiceCollection Services => (ServiceCollection)this["services"];

        IServiceConfiguration IRestClientConfiguration.Services => Services;
    }
}
