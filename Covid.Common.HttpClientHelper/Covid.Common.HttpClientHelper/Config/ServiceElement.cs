using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Config
{
    [DataContract]
    [Serializable]
    public class ServiceElement : ConfigurationElement, IServiceConfiguration
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        [JsonProperty("name")]
        public string Name => (string)base["name"];

        [ConfigurationProperty("rootUri", IsRequired = true)]
        [JsonProperty("rootUri")]
        public string RootUri => (string)base["rootUri"];

        [ConfigurationProperty("certificate", IsRequired = true)]
        [JsonProperty("certificate")]
        public string Certificate => (string)base["certificate"];

        [ConfigurationProperty("authentication", IsRequired = true)]
        [JsonProperty("authentication")]
        public AuthenticationElement Authentication => (AuthenticationElement)base["authentication"];
    }
}
