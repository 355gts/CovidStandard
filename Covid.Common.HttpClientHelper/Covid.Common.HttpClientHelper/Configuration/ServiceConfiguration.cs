using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [DataContract]
    [Serializable]
    public class ServiceConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("services", IsRequired = false, IsDefaultCollection = true)]
        [JsonProperty("services")]
        public ServiceCollection Services
        {
            get { return (ServiceCollection)base["services"]; }
        }
    }
}
