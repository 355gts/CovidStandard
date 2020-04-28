using Covid.Common.HttpClientHelper.Configuration;
using Covid.Rabbit.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace DotNetCoreProjectConvertor.Helpers
{
    public class MigrationHelper : IMigrationHelper
    {
        private readonly IEnumerable<string> _fileTypes = new List<string>() { "cs", "xml" };
        private readonly IProcessHelper _processHelper;
        private readonly string _outputDirectory;

        public MigrationHelper(IProcessHelper processHelper, string outputDirectory)
        {
            _processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        }

        public bool CreateSolution(string solutionName)
        {
            string args = $"new sln -n {solutionName} -o \"{_outputDirectory}\"";
            return _processHelper.ExecuteProcess("dotnet", $"new sln -n {solutionName} -o \"{_outputDirectory}\"");
        }

        public bool GenerateProject(string projectName, string projectType)
        {
            string args = $"new {projectType} -n {projectName} -o \"{Path.Combine(_outputDirectory, projectName)}\"";
            return _processHelper.ExecuteProcess("dotnet", args);
        }

        public bool AddProjectToSolution(string solutionName, string projectPath, string virtualPath)
        {
            var location = string.IsNullOrEmpty(virtualPath) ? "--in-root" : $"--solution-folder \"{virtualPath}\"";
            string args = $"sln \"{Path.Combine(_outputDirectory, solutionName)}\" add \"{projectPath}\" {location}";
            return _processHelper.ExecuteProcess("dotnet", args);
        }

        public bool MigrateFiles(string solutionName, string existingProjectPath, string projectName)
        {
            try
            {
                var projectFiles = Directory.GetFiles(existingProjectPath, "*.*", SearchOption.AllDirectories)
                    .Where(p => !p.Contains(@"\bin") && !p.Contains(@"\obj") && !p.Contains(@"AssemblyInfo"))
                    .Where(p => _fileTypes.Contains(p.Split('.').Last()));

                foreach (var file in projectFiles)
                {
                    var destinationFileName = file.Replace(existingProjectPath, Path.Combine(_outputDirectory, projectName, projectName));
                    if (destinationFileName.Contains(@"\"))
                    {
                        var destinationDirectory = destinationFileName.Substring(0, destinationFileName.LastIndexOf(@"\"));

                        if (!Directory.Exists(destinationDirectory))
                            Directory.CreateDirectory(destinationDirectory);
                    }

                    File.Copy(file, destinationFileName, true);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred copying files - '{ex.Message}'.");
            }
            return true;

        }

        public bool TranslateConfigFiles(string solutionName, string existingProjectPath, string projectName)
        {
            try
            {
                // TODO: use Release built configs to migrate environment specific configs settings across
                var configFiles = projectName.ToLower().Contains(".api")
                    ? new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("web.Config" , "appsettings.json"),
                        //new KeyValuePair<string, string>("web.Release.Config" , "appsettings.Release.json")
                    }
                    : new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("app.Config" , "appsettings.json"),
                        //new KeyValuePair<string, string>("app.Release.Config" , "appsettings.Release.json")
                    };

                foreach (var configFile in configFiles)
                {
                    var configFileName = $"{Path.Combine(existingProjectPath)}\\{configFile.Key}";
                    if (!File.Exists(configFileName))
                        return false;

                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", configFileName);
                    ResetConfigMechanism();
                    ConfigurationManager.RefreshSection("queueConfiguration");
                    ConfigurationManager.RefreshSection("restClients");
                    var queueConfig = ConfigurationManager.GetSection("queueConfiguration") as QueueConfiguration;
                    var serviceConfig = ConfigurationManager.GetSection("restClients") as ServiceConfiguration;

                    if (queueConfig != null)
                    {
                        string appSettingsJson = JsonConvert.SerializeObject(new
                        {
                            queueConfiguration = queueConfig,
                            services = serviceConfig.Services,
                            appSettings = MigrateAppSettings(solutionName, existingProjectPath, projectName),
                        }, new JsonSerializerSettings()
                        {
                            StringEscapeHandling = StringEscapeHandling.Default,
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore,
                            ContractResolver = new DefaultContractResolver
                            {
                                NamingStrategy = new CamelCaseNamingStrategy()
                            },
                        });

                        var appSettingsPath = $"{Path.Combine(_outputDirectory, projectName, projectName)}\\{configFile.Value}";
                        using (TextWriter tw = new StreamWriter(appSettingsPath, false))
                        {
                            tw.Write(appSettingsJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred copying files - '{ex.Message}'.");
                return false;
            }
            return true;

        }

        public object MigrateAppSettings(string solutionName, string existingProjectPath, string projectName)
        {
            try
            {
                // TODO: use Release built configs to migrate environment specific configs settings across
                var configFiles = projectName.ToLower().Contains(".api")
                    ? new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("web.Config" , "appsettings.json"),
                        //new KeyValuePair<string, string>("web.Release.Config" , "appsettings.Release.json")
                    }
                    : new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("app.Config" , "appsettings.json"),
                        //new KeyValuePair<string, string>("app.Release.Config" , "appsettings.Release.json")
                    };

                foreach (var configFile in configFiles)
                {
                    var configFileName = $"{Path.Combine(existingProjectPath)}\\{configFile.Key}";
                    if (!File.Exists(configFileName))
                        return null;

                    XDocument existingCsProjXml = XDocument.Load(configFileName);

                    var appSettings = existingCsProjXml.Descendants("configuration").Descendants("applicationSettings")
                                                         .Descendants().Where(x => x.Name.ToString().EndsWith("Settings"))
                                                         .Select(x => x.Descendants()).ToList();

                    List<KeyValuePair<string, string>> keyValues = new List<KeyValuePair<string, string>>();

                    foreach (var appSettingSection in appSettings)
                    {
                        var settings = (appSettingSection.Select(s => s).ToList())
                                                   .Where(s => s.Attribute("name") != null)
                                                   .Select(s => new KeyValuePair<string, string>(s.Attribute("name")?.Value, s.Attribute("serializeAs").Value == "Xml" ? $"[{string.Join(",", s.Value)}]" : s.Value));

                        keyValues.AddRange(settings);

                        //var settings = (appSettingSection.Select(s => s).ToList())
                        //                           .Where(s => s.Attribute("name") != null)
                        //                           .Select(s => $"\"{s.Attribute("name")?.Value}\": \"{s.Value}\"");

                    }

                    var settingsJson = JsonConvert.DeserializeObject("{" + string.Join(",", keyValues.Select(s => $"\"{s.Key}\": \"{s.Value}\"")) + "}", new JsonSerializerSettings()
                    {
                        StringEscapeHandling = StringEscapeHandling.Default,
                        Formatting = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        },
                    });

                    return settingsJson;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred copying files - '{ex.Message}'.");
                return null;
            }
            return null;

        }

        public bool ReplicateProjectReferences(string solutionName, string existingProjectPath, string projectName)
        {
            try
            {
                var existingCsProjPath = $"{Path.Combine(existingProjectPath, projectName)}.csproj";
                var newCsProjPath = $"{Path.Combine(_outputDirectory, projectName, projectName, projectName)}.csproj";

                XDocument existingCsProjXml = XDocument.Load(existingCsProjPath);
                XDocument newCsProjXml = XDocument.Load(newCsProjPath);

                var projectReferences = existingCsProjXml.Descendants().Where(x => x.Name.ToString().Contains("ItemGroup"))
                                                     .Descendants().Where(x => x.Name.ToString().Contains("ProjectReference"))
                                                     .Select(x => x);

                var referenceElement = new XElement("ItemGroup");

                foreach (var projectReference in projectReferences)
                {
                    var projectReferenceElement = new XElement("ProjectReference");
                    projectReferenceElement.Add(new XAttribute("Include", projectReference.Attribute("Include").Value));
                    referenceElement.Add(projectReferenceElement);
                }

                newCsProjXml.Root.Add(referenceElement);

                newCsProjXml.Save(newCsProjPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred replicating project references - '{ex.Message}'.");
                return false;
            }

            return true;
        }

        public bool GenerateSettingImplementations(string projectName)
        {
            try
            {
                var nameSpace = projectName.Substring(0, projectName.LastIndexOf("."));
                var outputPath = $"{Path.Combine(_outputDirectory, projectName, projectName)}\\Settings";
                var fileName = projectName.Substring(projectName.IndexOf(".")).Replace(".", string.Empty);
                var csFilename = $"{outputPath}\\{fileName}Settings.cs";
                var interfaceFilename = $"{outputPath}\\I{fileName}Settings.cs";

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                using (TextWriter tw = new StreamWriter(interfaceFilename))
                {
                    tw.WriteLine("namespace " + nameSpace + @".Settings");
                    tw.WriteLine("{");
                    tw.WriteLine("    public interface I" + fileName + @"Settings");
                    tw.WriteLine("    {");
                    tw.WriteLine("        // TODO: Implement settings from appsettings.json");
                    tw.WriteLine("    }");
                    tw.WriteLine("}");
                }

                using (TextWriter tw = new StreamWriter(csFilename))
                {
                    tw.WriteLine("namespace " + nameSpace + @".Settings");
                    tw.WriteLine("{");
                    tw.WriteLine("    public class " + fileName + @"Settings : I" + fileName + @"Settings");
                    tw.WriteLine("    {");
                    tw.WriteLine("        // TODO: Implement settings from appsettings.json");
                    tw.WriteLine("    }");
                    tw.WriteLine("}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred generating settings implementations - '{ex.Message}'.");
                return false;
            }

            return true;

        }

        private static void ResetConfigMechanism()
        {
            BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
            typeof(ConfigurationManager)
                .GetField("s_initState", Flags)
                .SetValue(null, 0);

            typeof(ConfigurationManager)
                .GetField("s_configSystem", Flags)
                .SetValue(null, null);

            typeof(ConfigurationManager)
                .Assembly.GetTypes()
                .Where(x => x.FullName == "System.Configuration.ClientConfigPaths")
                .First()
                .GetField("s_current", Flags)
                .SetValue(null, null);
            return;
        }
    }
}
