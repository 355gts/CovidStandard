using System;
using System.Diagnostics;

namespace DotNetCoreProjectConvertor.Helpers
{
    public class ProcessHelper : IProcessHelper
    {
        public bool ExecuteProcess(string processName, string arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = processName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = @"D:\CovidStandard\DotNetCoreProjectConvertor";
            process.OutputDataReceived += (sender, data) =>
            {
                Console.WriteLine(data.Data);
            };
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (sender, data) =>
            {
                Console.WriteLine(data.Data);
            };
            var result = process.Start();
            process.WaitForExit();

            return result;
        }
    }
}
