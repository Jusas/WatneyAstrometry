using System;
using System.IO;
using System.Text;
#pragma warning disable CS1591

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Default chatty logger implementation.
    /// </summary>
    public class DefaultVerboseLogger : IVerboseLogger
    {
        private static Encoding _encoding = new UTF8Encoding(false);

        public class Options
        {
            public bool WriteToStdout { get; set; }
            public bool WriteToFile { get; set; }
            public string LogFile { get; set; }
            public bool Enabled { get; set; }
        }

        private readonly object _mutex = new object();
        private Options _options;
        public DefaultVerboseLogger(Options options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private void WriteAny(string type, string message)
        {
            if (!_options.Enabled || (!_options.WriteToFile && !_options.WriteToStdout))
                return;

            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            message = $"[{time}] [{type}] {message}";

            if (_options.WriteToStdout)
                Console.WriteLine(message);
            if (_options.WriteToFile)
            {
                lock (_mutex)
                    File.AppendAllText(_options.LogFile, message + "\n", _encoding);
            }
        }

        public void Write(string message)
        {
            WriteInfo(message);
        }
        
        public void WriteInfo(string message)
        {
            WriteAny("INFO", message);
        }

        public void WriteWarn(string message)
        {
            WriteAny("WARN", message);
        }

        public void WriteError(string message)
        {
            WriteAny("ERROR", message);
        }
    }
}