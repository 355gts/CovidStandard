namespace Covid.Common.HttpClientHelper.Configuration
{
    public interface IServiceCollection
    {
        Service this[int idx] { get; }
    }
}