using System;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace RestoreRunner
{
    public class RestoreRunner
    {
        public void RunRestore()
        {  
            var logger = new BasicLogger();

            try
            {
                string projectFileName = @"C:\gr-oss\GRLargeApp\Solution.sln"; // <--- Change here can be another
                var projectCollection = new ProjectCollection();
                // logger.Verbosity = LoggerVerbosity.Diagnostic;
                var buildParameters = new BuildParameters(projectCollection)
                {
                    Loggers = new List<Microsoft.Build.Framework.ILogger> {logger},
                };

                var globalProperty = new Dictionary<String, String>
                {
                };

                BuildManager.DefaultBuildManager.ResetCaches();

                var buildRequest =
                    new BuildRequestData(projectFileName, globalProperty, null, new [] {"Restore"}, null);
                var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
                if (buildResult.OverallResult == BuildResultCode.Failure)
                {
                    throw new Exception("Build failed");
                }
            }
            finally
            {
                Console.WriteLine(logger.GetLogString());
            }
        }
    }
}