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
        static void Main(string[] args)
        {
            RegisterSDK();

            new RestoreRunner().RunRestore(args[0]);
        }

        static void RegisterSDK()
        {
            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var sdkDst = Path.Combine(currentDir, @"sdk\");
            var sdkSrc = MSBuildLocator.QueryVisualStudioInstances().First().MSBuildPath;

            Console.WriteLine($"Copying sdk from '{sdkDst}'.");
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
    }
}