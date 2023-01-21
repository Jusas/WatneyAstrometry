using System;
using System.Collections.Concurrent;
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

        private static ConcurrentQueue<string> _writeQueue = new ConcurrentQueue<string>();

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
                _writeQueue.Enqueue(message + Environment.NewLine);
                if (_writeQueue.Count > 50_000)
                {
                    lock (_mutex)
                    {
                        Flush();
                    }
                }
            }
        }

        /// <summary>
        /// Flushes the log lines from memory to file.
        /// This must be called when WriteToFile == true; until this is called,
        /// the logged lines are just kept in memory, with the exception that every 50_000 lines,
        /// Flush gets called automatically to release some memory.
        /// </summary>
        public void Flush()
        {
            if (_options.WriteToFile && !string.IsNullOrEmpty(_options.LogFile))
            {
                using (var stream = File.Open(_options.LogFile, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    string line;
                    byte[] bytes;
                    while (_writeQueue.TryDequeue(out line))
                    {
                        bytes = _encoding.GetBytes(line);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
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