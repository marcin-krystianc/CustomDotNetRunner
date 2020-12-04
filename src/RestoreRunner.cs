using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using NuGet.Protocol;

namespace RestoreRunner
{
    public class RestoreRunner
    {
        public void RunRestore(string projectPath)
        {  
            var logger = new BasicLogger();

            try
            {
                var projectCollection = new ProjectCollection();
                // logger.Verbosity = LoggerVerbosity.Diagnostic;
                var buildParameters = new BuildParameters(projectCollection)
                {
                    Loggers = new List<ILogger> {logger},
                };

                var globalProperties = new Dictionary<String, String>
                {
                    {"RestoreDisableParallel", "true"},
                    {"RestoreForce", "true"},
                };
                
                var target = "Restore";
                Console.WriteLine(
                    $"Running target:'{target}', projectPath:'{projectPath}', globalProperties:'{globalProperties.ToJson()}'");
                
                BuildManager.DefaultBuildManager.ResetCaches();

                var buildRequest = new BuildRequestData(projectPath, globalProperties, null, new[] {target}, null);
                var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
                if (buildResult.OverallResult == BuildResultCode.Failure)
                {
                    throw new Exception("Build failed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(logger.GetLogString());
            }
        }
    }
}