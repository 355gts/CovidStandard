using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [DataContract]
    [Serializable]
    public class Authentication : ConfigurationElement, IAuthentication
    {
        [ConfigurationProperty("type", IsRequired = true, IsKey = true)]
        [JsonProperty("type")]
        public string Type
        {
            get { return (string)base["type"]; }
            set { base["type"] = value; }
        }

        [ConfigurationProperty("certificateSubjectName", IsRequired = false, IsKey = true)]
        [JsonProperty("certificateSubjectName")]
        public string CertificateSubjectName
        {
            get { return (string)base["certificateSubjectName"]; }
            set { base["certificateSubjectName"] = value; }
        }
    }
}
