using System.IO;
using WatneyAstrometry.Core.Fits;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class FitsReaderTests
    {
        [Fact]
        public void Should_detect_valid_fits()
        {
            var fitsFile = "Resources/fits/m31.fits";
            var jpgFile = "Resources/jpg/heart-nebula.jpg";

            Assert.True(DefaultFitsReader.IsSupported(fitsFile), "Expected true (a valid fits file)");
            Assert.False(DefaultFitsReader.IsSupported(jpgFile), "Expected false (not a fits file)");
        }

        [Fact]
        public void Should_read_fits_correctly()
        {
            var sourceFile = "Resources/fits/m31.fits";
            var fitsReader = new DefaultFitsReader();

            var image = fitsReader.FromFile(sourceFile);
            Assert.True(5760 == image.PixelDataStreamOffset, "Expected two header blocks, 2 * 2880");
            Assert.True(image.Metadata.ImageWidth == 4656, "Image width has unexpected value");
            Assert.True(image.Metadata.ImageHeight == 3520, "Image height has unexpected value");
            Assert.True(image.Metadata.BitsPerPixel == 16, "Image bpp has unexpected value");
            image.Dispose();
        }

        [Fact]
        public void Should_read_fits_with_thumbnail()
        {
            // Should be able to handle it, but also ignoring it; PixInsight should save the thumb as a second image, and we don't
            // care about anything else than the first image (primary HDU).
            var sourceFile = "Resources/fits/ngc383.fits";
            var fitsReader = new DefaultFitsReader();

            var image = fitsReader.FromFile(sourceFile);
            Assert.True(image.Metadata.ImageWidth == 4656, "Image width has unexpected value");
            Assert.True(image.Metadata.ImageHeight == 3520, "Image height has unexpected value");
            image.Dispose();
        }

        [Fact]
        public void Should_read_fits_from_stream()
        {
            var sourceFile = "Resources/fits/m31.fits";
            using (var fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var fitsReader = new DefaultFitsReader();

                var image = fitsReader.FromStream(fs);
                Assert.True(image.Metadata.ImageWidth == 4656, "Image width has unexpected value");
                Assert.True(image.Metadata.ImageHeight == 3520, "Image height has unexpected value");
                image.Dispose();
            }

        }

    }
}
