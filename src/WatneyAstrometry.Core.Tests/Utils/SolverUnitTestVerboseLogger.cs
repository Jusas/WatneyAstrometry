using WatneyAstrometry.Core.Types;
using Xunit.Abstractions;

namespace WatneyAstrometry.Core.Tests.Utils
{
    public class SolverUnitTestVerboseLogger : IVerboseLogger
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SolverUnitTestVerboseLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void Write(string message)
        {
            _testOutputHelper.WriteLine("INFO: " + message);
        }

        public void WriteInfo(string message)
        {
            _testOutputHelper.WriteLine("INFO: " + message);
        }

        public void WriteWarn(string message)
        {
            _testOutputHelper.WriteLine("WARN: " + message);
        }

        public void WriteError(string message)
        {
            _testOutputHelper.WriteLine("ERROR: " + message);
        }

        public void Flush()
        {
            
        }
    }
}