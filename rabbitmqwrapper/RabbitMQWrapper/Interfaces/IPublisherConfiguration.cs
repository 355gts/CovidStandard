namespace RabbitMQWrapper.Interfaces
{
    public interface IPublisherConfiguration
    {
        string Name { get; }

        string ExchangeName { get; }

        string RoutingKey { get; }

        bool PublishesPersistentMessages { get; }
    }
}
