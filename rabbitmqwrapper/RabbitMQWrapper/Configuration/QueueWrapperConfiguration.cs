using RabbitMQWrapper.Interfaces;
using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using System.Web.Configuration;
using static RabbitMQWrapper.Properties.Resources;

namespace RabbitMQWrapper.Configuration
{
    public sealed class QueueWrapperConfiguration : ConfigurationSection, IQueueWrapperConfiguration
    {
        private const string sectionName = "queueWrapper";

        [ConfigurationProperty("uri", IsRequired = true)]
        public Uri Uri => (Uri)this["uri"];

        [ConfigurationProperty("clientCertificateSubjectName", IsRequired = true)]
        public string ClientCertificateSubjectName => (string)this["clientCertificateSubjectName"];

        [ConfigurationProperty("temporaryQueueNamePrefix", DefaultValue = "TMP_", IsRequired = false)]
        public string TemporaryQueueNamePrefix => (string)this["temporaryQueueNamePrefix"];

        [ConfigurationProperty("messagePrefetchCount", DefaultValue = (ushort)1, IsRequired = false)]
        public ushort MessagePrefetchCount => (ushort)this["messagePrefetchCount"];

        [ConfigurationProperty("rabbitMQHeartbeatSeconds", DefaultValue = (ushort)120, IsRequired = false)]
        public ushort RabbitMQHeartbeatSeconds => (ushort)this["rabbitMQHeartbeatSeconds"];

        [ConfigurationProperty("publishMessageConfirmationTimeoutSeconds", DefaultValue = 5, IsRequired = false)]
        public int PublishMessageConfirmationTimeoutSeconds => (int)this["publishMessageConfirmationTimeoutSeconds"];

        [ConfigurationProperty("millisecondsBetweenConnectionRetries", DefaultValue = 1000, IsRequired = false)]
        public int MillisecondsBetweenConnectionRetries => (int)this["millisecondsBetweenConnectionRetries"];

        [ConfigurationProperty("automaticRecoveryEnabled", DefaultValue = false, IsRequired = false)]
        public bool AutomaticRecoveryEnabled => (bool)this["automaticRecoveryEnabled"];

        [ConfigurationProperty("networkRecoveryIntervalSeconds", DefaultValue = 5, IsRequired = false)]
        public int NetworkRecoveryIntervalSeconds => (int)this["networkRecoveryIntervalSeconds"];

        [ConfigurationProperty("channelConfirmTimeoutIntervalSeconds", DefaultValue = 1, IsRequired = false)]
        public int ChannelConfirmTimeoutIntervalSeconds => (int)this["channelConfirmTimeoutIntervalSeconds"];

        [ConfigurationProperty("protocolTimeoutIntervalSeconds", DefaultValue = 20, IsRequired = false)]
        public int ProtocolTimeoutIntervalSeconds => (int)this["protocolTimeoutIntervalSeconds"];

        [ConfigurationProperty("consumers", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ConsumersCollection), AddItemName = "add")]
        private ConsumersCollection Consumers => (ConsumersCollection)this["consumers"];

        IConsumerConfigurations IQueueWrapperConfiguration.Consumers => Consumers;

        [ConfigurationProperty("publishers")]
        [ConfigurationCollection(typeof(PublishersCollection), AddItemName = "add")]
        private PublishersCollection Publishers => (PublishersCollection)this["publishers"];

        IPublisherConfigurations IQueueWrapperConfiguration.Publishers => Publishers;

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public static QueueWrapperConfiguration GetSection()
        {
            System.Configuration.Configuration config = null;

            if (HttpContext.Current != null)
            {
                config = WebConfigurationManager.OpenWebConfiguration("~");
            }
            else
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }

            var section = config.GetSection(sectionName) as QueueWrapperConfiguration;

            if (section == null)
                throw new ConfigurationErrorsException(string.Format(ConfigurationSectionNotFoundError, sectionName));

            return section;
        }
    }
}
