namespace RabbitMQWrapper.Interfaces
{
    public interface IConsumerConfiguration
    {
        string Name { get; }

        string QueueName { get; }

        string ExchangeName { get; }

        string RoutingKey { get; }

        int MessageWaitTimeoutMilliseconds { get; }
    }
}
