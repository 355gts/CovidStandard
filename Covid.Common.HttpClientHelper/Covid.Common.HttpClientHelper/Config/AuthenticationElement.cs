using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Runtime.Serialization;

namespace Covid.Common.HttpClientHelper.Config
{
    [DataContract]
    [Serializable]
    public class AuthenticationElement : ConfigurationElement, IAuthenticationConfiguration
    {
        [ConfigurationProperty("type", IsRequired = true, IsKey = true)]
        [JsonProperty("type")]
        public string Type => (string)base["type"];

        [ConfigurationProperty("certificateSubjectName", IsRequired = false)]
        [JsonProperty("certificateSubjectName")]
        public string CertificateSubjectName => (string)base["certificateSubjectName"];
    }
}
