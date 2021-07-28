using System;
using System.IO;
using System.Text;

namespace WatneyAstrometry.Core.Types
{
    public class DefaultVerboseLogger : IVerboseLogger
    {

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

        public void Write(string message)
        {
            if (!_options.Enabled || (!_options.WriteToFile && !_options.WriteToStdout))
                return;

            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            message = $"[{time}] {message}";

            if(_options.WriteToStdout)
                Console.WriteLine(message);
            if (_options.WriteToFile)
            {
                lock(_mutex)
                    File.AppendAllText(_options.LogFile, message + "\n", Encoding.UTF8);
            }

        }
    }
}