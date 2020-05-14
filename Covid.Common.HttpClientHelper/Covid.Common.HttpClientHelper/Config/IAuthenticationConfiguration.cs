namespace Covid.Common.HttpClientHelper.Config
{
    public interface IAuthenticationConfiguration
    {
        string Type { get; }

        string CertificateSubjectName { get; }
    }
}
