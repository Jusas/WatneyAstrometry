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
            _testOutputHelper.WriteLine(message);
        }
    }
}