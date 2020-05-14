namespace Covid.Common.HttpClientHelper.Config
{
    public interface IServiceConfiguration
    {
        string Name { get; }

        string RootUri { get; }

        string Certificate { get; }

        AuthenticationElement Authentication { get; }
    }
}
