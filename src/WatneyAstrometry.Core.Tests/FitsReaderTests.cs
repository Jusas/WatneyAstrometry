using System.IO;
using FluentAssertions;
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

            DefaultFitsReader.IsSupported(fitsFile).Should().BeTrue("A valid fits file");
            DefaultFitsReader.IsSupported(jpgFile).Should().BeFalse("Not a fits file");
        }

        [Fact]
        public void Should_read_fits_correctly()
        {
            var sourceFile = "Resources/fits/m31.fits";
            var fitsReader = new DefaultFitsReader();

            var image = fitsReader.FromFile(sourceFile);
            image.PixelDataStreamOffset.Should().Be(5760, "Two header blocks, 2 * 2880");
            image.Metadata.ImageWidth.Should().Be(4656);
            image.Metadata.ImageHeight.Should().Be(3520);
            image.Metadata.BitsPerPixel.Should().Be(16);
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
            image.Metadata.ImageWidth.Should().Be(4656);
            image.Metadata.ImageHeight.Should().Be(3520);
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
                image.Metadata.ImageWidth.Should().Be(4656);
                image.Metadata.ImageHeight.Should().Be(3520);
                image.Dispose();
            }

        }

    }
}
