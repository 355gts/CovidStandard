namespace Covid.Common.HttpClientHelper.Configuration
{
    public interface IRestClientConfiguration
    {
        IServiceConfiguration Services { get; set; }
    }
}
