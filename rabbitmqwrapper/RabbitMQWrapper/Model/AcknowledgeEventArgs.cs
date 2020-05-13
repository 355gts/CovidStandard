using System;
using System.Collections.Concurrent;

namespace RabbitMQWrapper.Model
{
    public class AcknowledgeEventArgs : EventArgs
    {
        public ConcurrentBag<ulong> DeliveryTags { get; set; }
        public Exception Exception { get; set; }
    }
}
