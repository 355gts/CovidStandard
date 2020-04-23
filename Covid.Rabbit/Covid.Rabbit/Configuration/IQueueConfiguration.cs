namespace Covid.Rabbit.Configuration
{
    public interface IQueueConfiguration
    {
        bool AutomaticRecoveryEnabled { get; set; }
        string CertificatePath { get; set; }
        QueueConfigCollection Consumers { get; }
        int ContinuationTimeoutSeconds { get; set; }
        int HandshakeContinuationTimeoutSeconds { get; set; }
        string Hostname { get; set; }
        ushort MaxPrefetchSize { get; set; }
        int NetworkRecoveryIntervalSeconds { get; set; }
        string Password { get; set; }
        int? Port { get; set; }
        QueueConfigCollection Publishers { get; }
        int RequestedConnectionTimeoutSeconds { get; set; }
        ushort RequestedHeartbeatSeconds { get; set; }
        string Uri { get; set; }
        string Username { get; set; }
    }
}