using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

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

                var globalProperty = new Dictionary<String, String>
                {
                    {"RestoreDisableParallel", "true"},
                };

                BuildManager.DefaultBuildManager.ResetCaches();

                var buildRequest = new BuildRequestData(projectPath, globalProperty, null, new[] {"Restore"}, null);
                var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
                if (buildResult.OverallResult == BuildResultCode.Failure)
                {
                    throw new Exception("Build failed");
                }
            }
            catch (Exception _)
            {
                Console.WriteLine(logger.GetLogString());
            }
        }
    }
}