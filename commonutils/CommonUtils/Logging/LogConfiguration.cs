using CommonUtils.Logging.Configuration;
using CommonUtils.Properties;
using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;

namespace CommonUtils.Logging
{
    public static class LogConfiguration
    {
        public static void Initialize(string applicationInstallDirectory = null)
        {
            var logConfiguration = Log4NetConfigurationManager.LoadConfiguration();

            GlobalContext.Properties["COMPONENT-NAME"] = logConfiguration.ComponentName;

            if (string.IsNullOrWhiteSpace(applicationInstallDirectory) 
                || !Directory.Exists(applicationInstallDirectory))
            {
                applicationInstallDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            }

            string log4netConfigPath = Path.Combine(applicationInstallDirectory, logConfiguration.ConfigurationFileName);

            // If we're debugging, and the log4net file doesn't exist in the root folder, check in the 'bin' folder.
            if (!File.Exists(log4netConfigPath))
            {
                log4netConfigPath = Path.Combine(applicationInstallDirectory, "bin", logConfiguration.ConfigurationFileName);
            }

            var logConfigFile = new FileInfo(log4netConfigPath);

            if (!logConfigFile.Exists)
            {                
                throw new ArgumentException(string.Format(
                    Resources.LoggingConfigurationFileNotFoundError,
                    logConfiguration.ConfigurationFileName,
                    applicationInstallDirectory));
            }

            XmlConfigurator.ConfigureAndWatch(logConfigFile);
        }
    }
}
