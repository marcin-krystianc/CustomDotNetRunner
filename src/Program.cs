using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CallContextProfiling;
using Microsoft.Build.Locator;
using Newtonsoft.Json;
using NuGet.Build.Tasks;

namespace RestoreRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var sdkPath = RegisterSDK();
            try
            {
                using (CallContextProfiler.NamedStep("Main"))
                {
                    // new DotNetRunner(sdkPath).RunDotNet(args);
                    new RestoreRunner().RunRestore(args[0]);
                }
            }
            finally
            {
                foreach (var d in CallContextProfiling.CallContextProfiler.Data
                    .OrderBy(x => x.Value.Elapsed)
                    .ThenBy(x => x.Key))
                {
                    Console.WriteLine($"{JsonConvert.SerializeObject(d)}");
                }
            }
        }

        static string RegisterSDK()
        {
            var branch = RunCommand("./../../../../NuGet.Client", "git", "rev-parse", "--abbrev-ref", "HEAD").Trim();
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var sdkDst = Path.Combine(currentDir, $"sdk-{branch}\\");
            var sdkSrc = MSBuildLocator.QueryVisualStudioInstances().First().MSBuildPath;

            Console.WriteLine($"Copying sdk from '{sdkSrc}'.");
            DirectoryCopy(sdkSrc, sdkDst, true);
            var filesToCopy = new DirectoryInfo(currentDir).GetFiles()
                .Select(x => x.Name)
                .Where(x => x.StartsWith("NuGet.", StringComparison.OrdinalIgnoreCase) ||
                            x.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Copying NuGet.* and Microsoft.* files from '{currentDir}'.");
            foreach (var fileToCopy in filesToCopy)
            {
                File.Copy(Path.Combine(currentDir, fileToCopy), Path.Combine(sdkDst, fileToCopy), true);
            }

            Console.WriteLine($"Registering sdk from '{sdkDst}'.");
            MSBuildLocator.RegisterMSBuildPath(sdkDst);

            // Copied from MSBuildLocator.RegisterInstance
            foreach (var keyValuePair in new Dictionary<string, string>()
            {
                ["MSBUILD_EXE_PATH"] = sdkDst + "MSBuild.dll",
                ["MSBuildExtensionsPath"] = sdkDst,
                ["MSBuildSDKsPath"] = sdkDst + "Sdks"
            })
            {
                Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
            }

            return sdkDst;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        static string RunCommand(string workingDirectory, string fileName, params string[] arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.WorkingDirectory = Path.GetFullPath(workingDirectory);

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("Process failed!:" + error);
            }

            return output;
        }
    }
}