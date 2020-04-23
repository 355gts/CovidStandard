using Covid.Api.App_Start;
using System.Configuration;
using System.Web.Http;

namespace Covid.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var connectionString = ConfigurationManager.ConnectionStrings["CovidApiContext"].ConnectionString;

            //Configure AutoFac  
            AutofacWebapiConfig.Initialize(GlobalConfiguration.Configuration, connectionString);
        }
    }
}
