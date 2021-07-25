using System;
using System.Collections.Generic;
using System.IO;
using WatneyAstrometry.Core.Image;
using Xunit;

namespace WatneyAstrometry.ImageReaders.Tests
{
    public class CommonImageReaderTests
    {
        [Fact]
        public void Should_read_pngs_to_monochrome_bytearray()
        {
            var results = new List<IImage>();
            CommonFormatsImageReader reader = new CommonFormatsImageReader();
            results.Add(reader.FromFile("./Resources/m81_16bit.png"));
            results.Add(reader.FromFile("./Resources/8bit-lum.png"));
            results.Add(reader.FromFile("./Resources/8bit-rgb.png"));
            results.Add(reader.FromFile("./Resources/16bit-lum.png"));
            results.Add(reader.FromFile("./Resources/16bit-rgb.png"));

            results.ForEach(r => r.Dispose());
        }


        [Fact]
        public void Should_read_jpgs_to_monochrome_bytearray()
        {
            var results = new List<IImage>();
            CommonFormatsImageReader reader = new CommonFormatsImageReader();
            results.Add(reader.FromFile("./Resources/m81_rgb8.jpg"));
            results.Add(reader.FromFile("./Resources/m81_gray8.jpg"));

            results.ForEach(r => r.Dispose());
        }

        [Fact]
        public void Should_read_jpgs_from_stream_to_bytearray()
        {
            var results = new List<IImage>();
            CommonFormatsImageReader reader = new CommonFormatsImageReader();

            using (FileStream fs = new FileStream("./Resources/m81_rgb8.jpg", FileMode.Open, FileAccess.Read))
            {
                results.Add(reader.FromStream(fs));
            }
            
            results.ForEach(r => r.Dispose());
        }
    }
}
