using DotNetCoreProjectConvertor.Extensions;
using DotNetCoreProjectConvertor.Helpers;
using System;
using System.IO;
using System.Linq;

namespace DotNetCoreProjectConvertor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any() || args.Length != 2)
            {
                Console.Write("Invalid input parameters detected.");
                Console.WriteLine("Param 1: Solution path and name");
                Console.WriteLine("Param 2: Output directory");
                Console.ReadKey();

                return;
            }

            var solutionPath = args[0];
            var outputDirectory = args[1];

            if (!File.Exists(solutionPath))
            {
                Console.WriteLine($"Cannot find '{solutionPath}'.");
                return;
            }

            var solutionFile = new FileInfo(solutionPath);
            var solutionRootPath = solutionFile.DirectoryName;

            if (Directory.Exists(outputDirectory) && (Directory.GetFiles(outputDirectory)).Any())
            {
                Console.WriteLine($"Output directory is not empty, aborting generation '{outputDirectory}'.");
                return;
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var processHelper = new ProcessHelper();
            var migrationHelper = new MigrationHelper(processHelper, outputDirectory);

            var result = migrationHelper.CreateSolution(solutionFile.Name.StripExtension());

            var projects = Directory.GetFiles(solutionRootPath, "*.csproj", SearchOption.AllDirectories)
                                    .Where(p => !p.Contains("Template") && !p.Contains("ProjectConvertor"));

            foreach (var project in projects)
            {
                var projectFile = new FileInfo(project);
                var projectType = GetProjectType(projectFile.Name);
                var projectNameNoExtension = projectFile.Name.StripExtension();

                result = migrationHelper.GenerateProject(projectFile.Name.StripExtension(), projectType);

                result = migrationHelper.ReplicateProjectReferences(solutionFile.Name.StripExtension(), projectFile.DirectoryName, projectNameNoExtension);

                result = migrationHelper.TranslateConfigFiles(solutionFile.Name.StripExtension(), projectFile.DirectoryName, projectNameNoExtension);

                result = migrationHelper.MigrateFiles(solutionFile.Name.StripExtension(), projectFile.DirectoryName, projectNameNoExtension);

                result = migrationHelper.AddProjectToSolution(solutionFile.Name, Path.Combine(outputDirectory, projectNameNoExtension, projectNameNoExtension, projectFile.Name), projectFile.Name.ToLower().Contains("test") ? "Tests" : null);
            }

            Console.WriteLine("Generated");
            Console.ReadKey();
        }

        private static string GetProjectType(string projectName)
        {
            if (projectName.ToLower().Contains("common"))
            {
                return projectName.ToLower().Contains("unittest") ? "covidClassLibraryUnitTest" : "covidClassLibrary";
            }

            if (projectName.ToLower().Contains("service"))
            {
                return projectName.ToLower().Contains("unittest") ? "covidServiceUnitTest" : "covidService";
            }

            if (projectName.ToLower().Contains("api"))
            {
                return projectName.ToLower().Contains("unittest") ? "covidApiUnitTest" : "covidApi";
            }

            return projectName.ToLower().Contains("unittest") ? "covidClassLibraryUnitTest" : "covidClassLibrary";

        }
    }
}
