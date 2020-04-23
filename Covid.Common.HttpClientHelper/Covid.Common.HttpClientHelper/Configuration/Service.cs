using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [DataContract]
    [Serializable]
    public class Service : ConfigurationElement, IService
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        [JsonProperty("name")]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("rootUri", IsRequired = true)]
        [JsonProperty("rootUri")]
        public string RootUri
        {
            get { return (string)base["rootUri"]; }
            set { base["rootUri"] = value; }
        }

        [ConfigurationProperty("certificate", IsRequired = true)]
        [JsonProperty("certificate")]
        public string Certificate
        {
            get { return (string)base["certificate"]; }
            set { base["certificate"] = value; }
        }

        [ConfigurationProperty("authentication", IsRequired = true)]
        [JsonProperty("authentication")]
        public Authentication Authentication
        {
            get { return (Authentication)base["authentication"]; }
            set { base["authentication"] = value; }
        }
    }
}
