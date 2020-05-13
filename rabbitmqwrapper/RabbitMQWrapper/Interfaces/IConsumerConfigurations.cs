namespace RabbitMQWrapper.Interfaces
{
    public interface IConsumerConfigurations
    {
        IConsumerConfiguration this[string name] { get; }
    }
}
