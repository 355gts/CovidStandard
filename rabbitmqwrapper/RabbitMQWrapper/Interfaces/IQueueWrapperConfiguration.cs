using System;

namespace RabbitMQWrapper.Interfaces
{
    public interface IQueueWrapperConfiguration
    {
        Uri Uri { get; }

        string ClientCertificateSubjectName { get; }

        string TemporaryQueueNamePrefix { get; }

        ushort MessagePrefetchCount { get; }

        ushort RabbitMQHeartbeatSeconds { get; }

        int PublishMessageConfirmationTimeoutSeconds { get; }

        int MillisecondsBetweenConnectionRetries { get; }

        IConsumerConfigurations Consumers { get; }

        IPublisherConfigurations Publishers { get; }
        bool AutomaticRecoveryEnabled { get; }

        int NetworkRecoveryIntervalSeconds { get; }

        int ChannelConfirmTimeoutIntervalSeconds { get; }

        int ProtocolTimeoutIntervalSeconds { get; }
    }
}
