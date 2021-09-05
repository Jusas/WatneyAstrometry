using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using VizUtils;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.StarDetection;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class StarDetectorTests
    {

        private class TestImage : Core.Image.IImage
        {
            public System.IO.Stream PixelDataStream {get; set; }
            public long PixelDataStreamOffset { get; set; }
            public long PixelDataStreamLength { get; set; }
            public Metadata Metadata { get; set; }

            public void Dispose()
            {
                PixelDataStream.Dispose();
            }
        }

        [Fact]
        public void Should_detect_5_stars()
        {
            var testSrc = TestImageUtils.GetPngByteStreamL8("Resources/png/test-5-stars.png");
            using(testSrc.stream)
            {
                var testImage = new TestImage
                {
                    PixelDataStream = testSrc.stream,
                    Metadata = new Metadata
                    {
                        BitsPerPixel = 8,
                        ImageWidth = testSrc.w,
                        ImageHeight = testSrc.h
                    }
                };
                var detector = new DefaultStarDetector();
                var stars = detector.DetectStars(testImage);

                //using (var outImage = new Image<Rgba32>(testSrc.w, testSrc.h))
                //{
                //    StarVisualizer.VisualizeStars(outImage, stars).SaveAsPng($"{nameof(Should_detect_5_stars)}.png");
                //}
                

                stars.Count.Should().Be(5);
            }
        }

        [Fact]
        public void Should_detect_8_stars()
        {
            var testSrc = TestImageUtils.GetPngByteStreamL8("Resources/png/cloudy-8-stars.png");
            using (testSrc.stream)
            {
                var testImage = new TestImage
                {
                    PixelDataStream = testSrc.stream,
                    Metadata = new Metadata
                    {
                        BitsPerPixel = 8,
                        ImageWidth = testSrc.w,
                        ImageHeight = testSrc.h
                    }
                };
                var detector = new DefaultStarDetector();
                var stars = detector.DetectStars(testImage);

                //using (var outImage = new Image<Rgba32>(testSrc.w, testSrc.h))
                //{
                //    StarVisualizer.VisualizeStars(outImage, stars).SaveAsPng($"{nameof(Should_detect_8_stars)}.png");
                //}


                stars.Count.Should().Be(8);
            }
        }
        
        [Fact]
        [Trait("Category", "Visual")]
        public void Visualize_star_pixel_bins()
        {
            using (var image = SixLabors.ImageSharp.Image.Load("Resources/jpg/other_7331.jpg"))
            {
                image.Mutate(ctx => ctx.Grayscale());
                var monoImage = image.CloneAs<L8>();
                var pixels = monoImage.GetPixelMemoryGroup()[0].Span;
                using (var stream = new MemoryStream(MemoryMarshal.AsBytes(pixels).ToArray()))
                {
                    var testImage = new TestImage
                    {
                        PixelDataStream = stream,
                        Metadata = new Metadata
                        {
                            BitsPerPixel = 8,
                            ImageWidth = image.Width,
                            ImageHeight = image.Height
                        }
                    };

                    var detector = new DefaultStarDetector();
                    detector.DetectStars(testImage);
                    var bins = detector.StarBins;

                    using (var outImage = new Image<Rgba32>(image.Width, image.Height, Color.Black))
                    {
                        StarVisualizer.VisualizeStarPixelBins(outImage, bins.ToArray()).SaveAsPng($"{nameof(Visualize_star_pixel_bins)}.png");
                    }
                }
                
                
                
            }
        }
    }
}