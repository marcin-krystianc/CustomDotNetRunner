using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CallContextProfiling;
using Microsoft.Build.Locator;
using Newtonsoft.Json;

namespace RestoreRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var sdkPath = RegisterSDK();
            try
            {
                //var sdkPath = @"c:\Program Files\dotnet\sdk\3.1.411\";
                //var sdkPath = @"c:\Program Files\dotnet\sdk\5.0.302\";
                //var sdkPath = @"d:\dotnet\sdk\6.0.100-transitivedependencypinning\";
                //var sdkPath = @"d:\dotnet\sdk\6.0.100-dev.1\";
                // Copied from MSBuildLocator.RegisterInstance
                foreach (var keyValuePair in new Dictionary<string, string>()
                {
                    ["MSBUILD_EXE_PATH"] = sdkPath + "MSBuild.dll",
                    ["MSBuildExtensionsPath"] = sdkPath,
                    ["MSBuildSDKsPath"] = sdkPath + "Sdks",
                    ["NUGET_PACKAGES"] = @"d:\global-packages",
                })
                {
                    Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
                }

                using (CallContextProfiler.NamedStep("Main"))
                {
                    AssemblyLoadContext.Default.Resolving += (context, name) =>
                        AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(sdkPath, name.Name + ".dll"));
                    
                    //new DotNetRunner(sdkPath).RunDotNet(args);
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
            var sdkDst = @"d:\dotnet\sdk\5.0.403-transitivedependencypinning\";

            var searchPattern = new[] { "NuGet*.dll", "NuGet*.pdb", "NuGet*.xml", "Newtonsoft.Json" };

            Console.WriteLine($"Sdk: '{sdkDst}'.");
            var filesToCopy = searchPattern.SelectMany(x => new DirectoryInfo(currentDir).GetFiles(x))
                .Select(x => x.Name)
                .OrderBy(x => x);

            foreach (var fileToCopy in filesToCopy)
            {
                Console.WriteLine($"Copying '{fileToCopy}' to '{sdkDst}'.");
                File.Copy(Path.Combine(currentDir, fileToCopy), Path.Combine(sdkDst, fileToCopy), true);
            }

            Console.WriteLine($"Registering sdk: '{sdkDst}'.");
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