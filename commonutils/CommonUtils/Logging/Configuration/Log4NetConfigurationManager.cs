using System.Configuration;

namespace CommonUtils.Logging.Configuration
{
    public static class Log4NetConfigurationManager
    {
        public static Log4NetConfigurationSection LoadConfiguration()
        {
            var configSection = ConfigurationManager.GetSection("log4net") as Log4NetConfigurationSection;

            if (configSection == null)
            {
                throw new ConfigurationErrorsException("Could not find configuration section 'log4net'.");
            }

            return configSection;
        }
    }
}
