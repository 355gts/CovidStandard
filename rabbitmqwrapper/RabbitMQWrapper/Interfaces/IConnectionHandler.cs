using RabbitMQ.Client;
using System;

namespace RabbitMQWrapper.Interfaces
{
    public interface IConnectionHandler : IDisposable
    {
        event EventHandler ConnectionLost;

        event EventHandler ConnectionRestored;

        IModel CreateModel();

        bool ConnectionShuttingDown { get; }
    }
}
