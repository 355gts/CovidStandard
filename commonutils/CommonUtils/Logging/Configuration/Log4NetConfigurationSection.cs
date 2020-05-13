using System.Configuration;

namespace CommonUtils.Logging.Configuration
{
    public sealed class Log4NetConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("log4netConfigurationFileName")]
        public string ConfigurationFileName => this["log4netConfigurationFileName"] as string;

        [ConfigurationProperty("componentName")]
        public string ComponentName => this["componentName"] as string;
    }
}
