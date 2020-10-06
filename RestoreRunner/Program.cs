using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Locator;
using NuGet.Build.Tasks;

namespace RestoreRunner
{
    class Program
    {
        private static string[] DllsToCopy = new string[] { 
            "NuGet.Build.Tasks.dll",
            "NuGet.Credentials.dll",
            "NuGet.Configuration.dll",
            "NuGet.Common.dll",
            "NuGet.Commands.dll",
            "NuGet.ProjectModel.dll",
            "NuGet.LibraryModel.dll",
            "NuGet.Packaging.dll",
            "NuGet.Versioning.dll",
            "NuGet.Protocol.dll",
            "NuGet.Frameworks.dll",
            "NuGet.DependencyResolver.Core.dll",
            "Microsoft.Build.dll",
            "Microsoft.Build.Framework.dll",
            //"Microsoft.Build.NuGetSdkResolver.dll",
            "Microsoft.Build.Tasks.Core.dll",
            "Microsoft.Build.Utilities.Core.dll"};

        static void Main(string[] args)
        {
            foreach (var instance in MSBuildLocator.QueryVisualStudioInstances())
            {
                Console.WriteLine($"MSBuildPath:{instance.MSBuildPath}, VisualStudioRootPath:{instance.VisualStudioRootPath}");
            }

            RegisterSDK();

            Run();
        }

        static void RegisterSDK()
        {
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var sdkDst = Path.Combine(currentDir, @"sdk\");
            var sdkSrc = MSBuildLocator.QueryVisualStudioInstances().First().MSBuildPath;
            DirectoryCopy(sdkSrc, sdkDst, true);
            foreach (var dll in DllsToCopy)
            {
                File.Copy(Path.Combine(currentDir, dll), Path.Combine(sdkDst, dll), true);
            }

            Console.WriteLine($"Registering:" + sdkDst);
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
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
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

        static void Run()
        {

            var restoreRunner = new RestoreRunner();
            restoreRunner.RunRestore();
        }
    }
}
