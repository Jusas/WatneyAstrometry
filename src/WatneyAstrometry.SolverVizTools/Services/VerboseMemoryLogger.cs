using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.SolverVizTools.Services
{

    public interface IVerboseMemoryLogger : IVerboseLogger
    {
        void Clear();
        IReadOnlyList<string> FullLog { get; }
    }

    public class VerboseMemoryLogger : IVerboseMemoryLogger
    {

        private readonly object _mutex = new object();
        private List<string> _logLines = new List<string>(10_000_000);
        public IReadOnlyList<string> FullLog => _logLines;

        public void Clear()
        {
            lock (_mutex)
                _logLines.Clear();
        }


        private void Write(string message, string type)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            message = $"[{time}] [{type}] {message}";

            lock (_mutex)
                _logLines.Add(message);
        }

        public void Write(string message)
        {
            Write(message, "INFO");
        }

        public void WriteInfo(string message)
        {
            Write(message, "INFO");
        }

        public void WriteWarn(string message)
        {
            Write(message, "WARN");
        }

        public void WriteError(string message)
        {
            Write(message, "ERROR");
        }
    }
}
