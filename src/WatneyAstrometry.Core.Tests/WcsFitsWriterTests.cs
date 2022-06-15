using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Types;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class WcsFitsWriterTests
    {

        [Fact]
        public void Should_format_long_doubles_to_fit_FITS_record_size()
        {
            using Stream stream = new MemoryStream();
            WcsFitsWriter writer = new WcsFitsWriter(stream);

            double longDouble = 3.4703595730434245E-07; // over 20 chars
            writer.WriteRecord("CD1_1", longDouble, "cd matrix");
            stream.Seek(0, SeekOrigin.Begin);

            var buf = new byte[80];
            var read = stream.Read(buf, 0, 80);

            Assert.Equal(80, read);
            var record = Encoding.ASCII.GetString(buf);

            var data = record.Split('/')[0].Trim();
            data.Should().EndWith("E-07");

        }
    }
}
