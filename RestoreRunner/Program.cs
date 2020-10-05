using System;
using Microsoft.Build.Locator;

namespace RestoreRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var instance in MSBuildLocator.QueryVisualStudioInstances())
            {
                Console.WriteLine($"MSBuildPath:{instance.MSBuildPath}, VisualStudioRootPath:{instance.VisualStudioRootPath}");
            }
            MSBuildLocator.RegisterDefaults();
            var restoreRunner = new RestoreRunner();
            restoreRunner.RunRestore();
        }
    }
}
