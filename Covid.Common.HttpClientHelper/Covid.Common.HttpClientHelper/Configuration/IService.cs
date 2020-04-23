namespace Covid.Common.HttpClientHelper.Configuration
{
    public interface IService
    {
        Authentication Authentication { get; set; }
        string Certificate { get; set; }
        string Name { get; set; }
        string RootUri { get; set; }
    }
}