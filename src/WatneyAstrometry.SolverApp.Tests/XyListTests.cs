using System.IO;
using Xunit;

namespace WatneyAstrometry.SolverApp.Tests
{
    public class XyListTests
    {
        [Fact]
        public void Should_get_a_proper_star_list_from_xyls()
        {
            using var data = File.OpenRead("Resources/m31_a.xyls");

            var xyList = XyList.FromStream(data);

            Assert.NotNull(xyList);

        }
    }
}