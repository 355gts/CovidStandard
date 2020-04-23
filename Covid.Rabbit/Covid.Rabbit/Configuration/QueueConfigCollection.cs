using System.Configuration;

namespace Covid.Rabbit.Configuration
{
    [ConfigurationCollection(typeof(QueueConfig))]
    public class QueueConfigCollection : ConfigurationElementCollection, IQueueConfigCollection
    {
        public QueueConfig this[int idx]
        {
            get { return (QueueConfig)BaseGet(idx); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new QueueConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((QueueConfig)(element)).Name;
        }
    }
}
