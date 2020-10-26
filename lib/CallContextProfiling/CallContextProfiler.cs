using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CallContextProfiling
{
    public static class CallContextProfiler
    {
        public static Dictionary<string, (int Calls, TimeSpan Elapsed)> Data = new Dictionary<string, (int, TimeSpan)>();

        public static IDisposable NamedStep(string name)
        {
            return new Step(name);
        }
    }

    internal class Step : IDisposable
    {
        private Stopwatch _sw = Stopwatch.StartNew();
        private string _name;
        public Step(string name)
        {
            _name = name;
        }

        public void Dispose()
        {
            _sw.Stop();
            var data = CallContextProfiler.Data;
            
            lock (data)
            {
                if (!data.TryGetValue(_name, out var val))
                {
                    val = (0, TimeSpan.Zero);
                }

                val.Calls++;
                val.Elapsed += _sw.Elapsed;
                data[_name] = val;
            }
        }
    }
}