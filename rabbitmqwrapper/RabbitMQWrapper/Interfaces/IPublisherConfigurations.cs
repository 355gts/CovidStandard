namespace RabbitMQWrapper.Interfaces
{
    public interface IPublisherConfigurations
    {
        IPublisherConfiguration this[string name] { get; }
    }
}
