namespace DotNetCoreProjectConvertor.Helpers
{
    public interface IMigrationHelper
    {
        bool CreateSolution(string solutionName);

        bool GenerateProject(string projectName, string projectType);

        bool AddProjectToSolution(string solutionName, string projectPath, string virtualPath);

        bool MigrateFiles(string solutionName, string existingProjectPath, string projectName);

        bool TranslateConfigFiles(string solutionName, string existingProjectPath, string projectName);

        bool ReplicateProjectReferences(string solutionName, string existingProjectPath, string projectName);
    }
}
