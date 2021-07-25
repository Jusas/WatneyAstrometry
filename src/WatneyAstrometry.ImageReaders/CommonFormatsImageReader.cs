using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core.Image;
using IImage = WatneyAstrometry.Core.Image.IImage;

namespace WatneyAstrometry.ImageReaders
{
    /// <summary>
    /// A common image format reader. Uses SixLabors.ImageSharp to do the heavy lifting.
    /// PNG and JPEG support, they're the most common and easy to implement.
    /// </summary>
    public class CommonFormatsImageReader : IImageReader
    {
        public static string[] SupportedImageExtensions => new [] {"png", "jpg", "jpeg"};

        public IImage FromFile(string filename)
        {

            if (!File.Exists(filename))
                throw new IOException($"File '{filename}' does not exist");

            using (var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if(filename.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                    return FromPng(fileStream);

                if (filename.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                    filename.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase))
                {
                    return FromJpeg(fileStream);
                }

            }
            
            throw new NotImplementedException("Unknown file extension, this image reader cannot read the file");
        }


        private void WriteImageBytesToStream<T>(Image<T> image, Stream stream) where T : unmanaged, IPixel<T>
        {
            if (image.TryGetSinglePixelSpan(out var pixels))
            {
                var buf = MemoryMarshal.AsBytes(pixels).ToArray();
                stream.Write(buf, 0, buf.Length);
            }
            else
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pixelRowSpan = image.GetPixelRowSpan(y);
                    var buf = MemoryMarshal.AsBytes(pixelRowSpan).ToArray();
                    stream.Write(buf, 0, buf.Length);
                }
            }
        }

        
        private IImage FromPng(Stream imageStream)
        {
            
            using (var image = Image.Load(imageStream))
            {
                int bitsPerPixel = 0;
                var pngMetadata = image.Metadata.GetPngMetadata();
                
                if (pngMetadata.ColorType == PngColorType.Grayscale)
                {
                    if (pngMetadata.BitDepth != PngBitDepth.Bit8 && pngMetadata.BitDepth != PngBitDepth.Bit16)
                        throw new NotImplementedException("Unhandled image bit depth");

                    var tempFile = Path.GetTempFileName();

                    if (pngMetadata.BitDepth == PngBitDepth.Bit8)
                    {
                        bitsPerPixel = 16;
                        using (var monoImage = image.CloneAs<L8>())
                        using (var tempFileStream = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                            WriteImageBytesToStream(monoImage, tempFileStream);
                    }
                    else if (pngMetadata.BitDepth == PngBitDepth.Bit16)
                    {
                        bitsPerPixel = 16;
                        using (var monoImage = image.CloneAs<L16>())
                        using (var tempFileStream = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                            WriteImageBytesToStream(monoImage, tempFileStream);
                    }
                    
                    return new RawByteImage(tempFile, true)
                    {
                        Metadata = new Metadata
                        {
                            ImageWidth = image.Width,
                            ImageHeight = image.Height,
                            BitsPerPixel = bitsPerPixel
                        }
                    };
                }

                if (pngMetadata.ColorType == PngColorType.Rgb || pngMetadata.ColorType == PngColorType.RgbWithAlpha)
                {

                    image.Mutate(x => x.Grayscale());
                    var tempFile = Path.GetTempFileName();

                    if (pngMetadata.BitDepth == PngBitDepth.Bit8)
                    {
                        using (var monoImage = image.CloneAs<L8>())
                        using (var tempFileStream = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                            WriteImageBytesToStream(monoImage, tempFileStream);
                        
                        bitsPerPixel = 8;
                    }
                    if (pngMetadata.BitDepth == PngBitDepth.Bit16)
                    {
                        using (var monoImage = image.CloneAs<L16>())
                        using (var tempFileStream = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                            WriteImageBytesToStream(monoImage, tempFileStream);
                        bitsPerPixel = 16;
                    }
                    
                    return new RawByteImage(tempFile, true)
                    {
                        Metadata = new Metadata
                        {
                            ImageWidth = image.Width,
                            ImageHeight = image.Height,
                            BitsPerPixel = bitsPerPixel
                        }
                    };

                }

                throw new NotImplementedException(
                    "PNG support is limited to 8-bit and 16-bit grayscale and RGB images without transparency.");
            }
        }

        private IImage FromJpeg(Stream imageStream)
        {
            // Just coerce to rgba32 and convert to mono.
            using (var image = Image.Load(imageStream))
            {
                int bitsPerPixel = 8;
                image.Mutate(x => x.Grayscale());
                var tempFile = Path.GetTempFileName();

                using (var monoImage = image.CloneAs<L8>())
                using (var tempFileStream = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                    WriteImageBytesToStream(monoImage, tempFileStream);

                return new RawByteImage(tempFile, true)
                {
                    Metadata = new Metadata
                    {
                        ImageWidth = image.Width,
                        ImageHeight = image.Height,
                        BitsPerPixel = bitsPerPixel
                    }
                };
            }
        }
        

        public IImage FromStream(Stream stream)
        {
            var imageFormat = Image.DetectFormat(stream);
            if (imageFormat == null)
                throw new ArgumentException("Stream does not contain an image in a supported format");

            if (imageFormat.FileExtensions.Contains("png"))
            {
                return FromPng(stream);
            }

            if (imageFormat.FileExtensions.Contains("jpg"))
            {
                return FromJpeg(stream);
            }

            throw new NotImplementedException("Unknown file type, this image reader cannot read the file");

        }

        public static bool IsSupported(Stream stream)
        {
            var imageFormat = Image.DetectFormat(stream);
            if (imageFormat == null)
                return false;

            if (imageFormat.FileExtensions.Contains("png") || imageFormat.FileExtensions.Contains("jpg"))
                return true;

            return false;
        }

        public static bool IsSupported(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                return IsSupported(fs);
        }
    }
}
