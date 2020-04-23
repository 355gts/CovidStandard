using System.Collections;

namespace Covid.Rabbit.Configuration
{
    public interface IQueueConfigCollection : ICollection, IEnumerable
    {
        QueueConfig this[int idx] { get; }
    }
}