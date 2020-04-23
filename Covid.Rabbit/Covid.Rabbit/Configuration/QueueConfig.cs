using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Rabbit.Configuration
{
    [DataContract]
    [Serializable]
    public class QueueConfig : ConfigurationElement, IQueueConfig
    {
        [ConfigurationProperty("name", IsRequired = false)]
        [JsonProperty("name")]
        public string Name
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        [ConfigurationProperty("queue", IsRequired = false)]
        [JsonProperty("queue")]
        public string Queue
        {
            get { return (string)base["queue"]; }
            set { base["queue"] = value; }
        }

        [ConfigurationProperty("routingKey", IsRequired = false)]
        [JsonProperty("routingKey")]
        public string RoutingKey
        {
            get { return (string)base["routingKey"]; }
            set { base["routingKey"] = value; }
        }

        [ConfigurationProperty("exchange", IsRequired = false)]
        [JsonProperty("exchange")]
        public string Exchange
        {
            get { return (string)base["exchange"]; }
            set { base["exchange"] = value; }
        }
    }
}
