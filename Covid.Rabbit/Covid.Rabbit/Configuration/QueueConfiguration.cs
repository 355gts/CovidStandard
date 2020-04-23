using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Rabbit.Configuration
{
    [DataContract]
    [Serializable]
    public class QueueConfiguration : ConfigurationSection, IQueueConfiguration
    {
        [ConfigurationProperty("hostname", IsRequired = true, IsKey = true)]
        [JsonProperty("hostname")]
        public string Hostname
        {
            get { return (string)base["hostname"]; }
            set { base["hostname"] = value; }
        }

        [ConfigurationProperty("username", IsRequired = true)]
        [JsonProperty("username")]
        public string Username
        {
            get { return (string)base["username"]; }
            set { base["username"] = value; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        [JsonProperty("password")]
        public string Password
        {
            get { return (string)base["password"]; }
            set { base["password"] = value; }
        }

        [ConfigurationProperty("uri", IsRequired = true)]
        [JsonProperty("uri")]
        public string Uri
        {
            get { return (string)base["uri"]; }
            set { base["uri"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = false)]
        [JsonProperty("port")]
        public int? Port
        {
            get { return (int?)base["port"]; }
            set { base["port"] = value; }
        }

        [ConfigurationProperty("certificatePath", IsRequired = false)]
        [JsonProperty("certificatePath")]
        public string CertificatePath
        {
            get { return (string)base["certificatePath"]; }
            set { base["certificatePath"] = value; }
        }

        [ConfigurationProperty("automaticRecoveryEnabled", IsRequired = false, DefaultValue = true)]
        [JsonProperty("automaticRecoveryEnabled")]
        public bool AutomaticRecoveryEnabled
        {
            get { return (bool)base["automaticRecoveryEnabled"]; }
            set { base["automaticRecoveryEnabled"] = value; }
        }

        [ConfigurationProperty("maxPrefetchSize", IsRequired = false, DefaultValue = (ushort)8)]
        [JsonProperty("maxPrefetchSize")]
        public ushort MaxPrefetchSize
        {
            get { return (ushort)base["maxPrefetchSize"]; }
            set { base["maxPrefetchSize"] = value; }
        }

        [ConfigurationProperty("networkRecoveryIntervalSeconds", IsRequired = false, DefaultValue = 10)]
        [JsonProperty("networkRecoveryIntervalSeconds")]
        public int NetworkRecoveryIntervalSeconds
        {
            get { return (int)base["networkRecoveryIntervalSeconds"]; }
            set { base["networkRecoveryIntervalSeconds"] = value; }
        }

        [ConfigurationProperty("continuationTimeoutSeconds", IsRequired = false, DefaultValue = 10)]
        [JsonProperty("continuationTimeoutSeconds")]
        public int ContinuationTimeoutSeconds
        {
            get { return (int)base["continuationTimeoutSeconds"]; }
            set { base["continuationTimeoutSeconds"] = value; }
        }

        [ConfigurationProperty("handshakeContinuationTimeoutSeconds", IsRequired = false, DefaultValue = 10)]
        [JsonProperty("handshakeContinuationTimeoutSeconds")]
        public int HandshakeContinuationTimeoutSeconds
        {
            get { return (int)base["handshakeContinuationTimeoutSeconds"]; }
            set { base["handshakeContinuationTimeoutSeconds"] = value; }
        }

        [ConfigurationProperty("requestedConnectionTimeoutSeconds", IsRequired = false, DefaultValue = 10)]
        [JsonProperty("requestedConnectionTimeoutSeconds")]
        public int RequestedConnectionTimeoutSeconds
        {
            get { return (int)base["requestedConnectionTimeoutSeconds"]; }
            set { base["requestedConnectionTimeoutSeconds"] = value; }
        }

        [ConfigurationProperty("requestedHeartbeatSeconds", IsRequired = false, DefaultValue = (ushort)10)]
        [JsonProperty("requestedHeartbeatSeconds")]
        public ushort RequestedHeartbeatSeconds
        {
            get { return (ushort)base["requestedHeartbeatSeconds"]; }
            set { base["requestedHeartbeatSeconds"] = value; }
        }

        [ConfigurationProperty("consumers", IsRequired = false)]
        [JsonProperty("consumers")]
        public QueueConfigCollection Consumers
        {
            get { return (QueueConfigCollection)base["consumers"]; }
        }

        [ConfigurationProperty("publishers", IsRequired = false)]
        [JsonProperty("publishers")]
        public QueueConfigCollection Publishers
        {
            get { return (QueueConfigCollection)base["publishers"]; }
        }
    }
}
