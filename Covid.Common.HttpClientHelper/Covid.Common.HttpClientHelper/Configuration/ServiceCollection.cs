using System.Configuration;

namespace Covid.Common.HttpClientHelper.Configuration
{
    [ConfigurationCollection(typeof(Service))]
    public class ServiceCollection : ConfigurationElementCollection, IServiceCollection
    {
        //public ServiceCollection()
        //{
        //    Service service = (Service)CreateNewElement();
        //    if (!string.IsNullOrEmpty(service.Name))
        //        Add(service);

        //}

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new Service();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Service)(element)).Name;
        }

        public Service this[int idx]
        {
            get
            {
                return (Service)BaseGet(idx);
            }
            set
            {
                if (BaseGet(idx) != null)
                    BaseRemoveAt(idx);
                BaseAdd(idx, value);
            }
        }

        //new public Service this[string name]
        //{
        //    get
        //    {
        //        return (Service)BaseGet(name);
        //    }
        //}

        //public int IndexOf(Service service)
        //{
        //    return BaseIndexOf(service);
        //}

        //public void Add(Service service)
        //{
        //    BaseAdd(service);
        //}

        //protected override void BaseAdd(ConfigurationElement element)
        //{
        //    base.BaseAdd(element, false);
        //}

        //public void Remove(Service service)
        //{
        //    if (BaseIndexOf(service) >= 0)
        //        BaseRemove(service.Name);
        //}

        //public void RemoveAt(int idx)
        //{
        //    BaseRemoveAt(idx);
        //}

        //public void Remove(string name)
        //{
        //    BaseRemove(name);
        //}

        //public void Clear()
        //{
        //    BaseClear();
        //}

        protected override string ElementName
        {
            get
            {
                return "service";
            }
        }
    }
}
