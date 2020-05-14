namespace Covid.Common.HttpClientHelper.Config
{
    public interface IServiceConfigurations
    {
        IServiceConfiguration this[string name] { get; }
    }
}
