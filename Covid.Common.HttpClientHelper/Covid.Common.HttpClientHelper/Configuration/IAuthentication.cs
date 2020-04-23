namespace Covid.Common.HttpClientHelper.Configuration
{
    public interface IAuthentication
    {
        string CertificateSubjectName { get; set; }
        string Type { get; set; }
    }
}