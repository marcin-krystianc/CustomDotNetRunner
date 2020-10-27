using System;
using System.IO;
using System.Runtime.Loader;

namespace RestoreRunner
{
    public class DotNetRunner
    {
        private string _sdk;

        public DotNetRunner(string sdkPath)
        {
            _sdk = sdkPath;
        }

        public void RunDotNet(string[] args)
        {
            var assemblyPath = Path.Combine(_sdk, "dotnet.dll");
            AssemblyLoadContext.Default.Resolving += (context, name) =>
                AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(_sdk, name.Name + ".dll"));
            AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", _sdk);
            var myAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            var programType = myAssembly.GetType("Microsoft.DotNet.Cli.Program");
            var main = programType.GetMethod("Main");
            dynamic programInstance = Activator.CreateInstance(programType);
            var result = main.Invoke(null, new object?[]{args});
        }
    }
}