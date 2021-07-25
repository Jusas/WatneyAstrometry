using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using WatneyAstrometry.Core.Fits;

namespace VizUtils
{
    public class TestImageUtils
    {

        public static unsafe Image<Rgba32> FitsImagePixelBufferToRgbaImage(FitsImage fitsImage)
        {
            var bpp = fitsImage.Metadata.BitsPerPixel;
            if (bpp == 8 || bpp == 16)
            {
                fitsImage.PixelDataStream.Seek(fitsImage.PixelDataStreamOffset, SeekOrigin.Begin);
                var buf = new byte[fitsImage.PixelDataStreamLength];
                fitsImage.PixelDataStream.Read(buf, 0, buf.Length);
                if (bpp == 8)
                {
                    using (var monoImage = Image.LoadPixelData<L8>(buf, fitsImage.Metadata.ImageWidth,
                        fitsImage.Metadata.ImageHeight))
                        return monoImage.CloneAs<Rgba32>();
                }
                else
                {
                    var byteIncrement = 2;
                    var convBuf = new byte[buf.Length];

                    // Seems like a little conversion is required here...

                    fixed (byte* pBuffer = buf)
                    {
                        for (var i = 0; i < buf.Length; i += byteIncrement)
                        {
                            short val = (short)(pBuffer[i] << 8 | pBuffer[i + 1]);
                            ushort uval = (ushort)(val - short.MinValue);
                            var bytes = BitConverter.GetBytes(uval);
                            convBuf[i] = bytes[0];
                            convBuf[i + 1] = bytes[1];
                        }
                    }

                    using (var monoImage = Image.LoadPixelData<L16>(convBuf, fitsImage.Metadata.ImageWidth,
                        fitsImage.Metadata.ImageHeight))
                        return monoImage.CloneAs<Rgba32>();
                }
            }

            throw new NotImplementedException("BPP 8 & 16 supported");
        }

        public static (Stream stream, int w, int h) GetPngByteStreamL8(string filename)
        {
            var decoder = new PngDecoder();
            var stream = File.OpenRead(filename);
            
            var imgInfo = decoder.Identify(Configuration.Default, stream);
            stream.Seek(0, SeekOrigin.Begin);
            
            if (imgInfo.PixelType.BitsPerPixel == 8)
            {
                var img = decoder.Decode<L8>(Configuration.Default, stream);
                var pixelSpan = img.GetPixelMemoryGroup().ToArray()[0].Span;
                return (new MemoryStream(MemoryMarshal.AsBytes(pixelSpan).ToArray()), img.Width, img.Height);
            }

            throw new ArgumentException("Not L8 png");

        }

        public static Stream GetPngByteStreamL16(string filename)
        {
            var decoder = new PngDecoder();
            var stream = File.OpenRead(filename);

            var imgInfo = decoder.Identify(Configuration.Default, stream);
            stream.Seek(0, SeekOrigin.Begin);

            if (imgInfo.PixelType.BitsPerPixel == 16)
            {
                var img = decoder.Decode<L16>(Configuration.Default, stream);
                var pixelSpan = img.GetPixelMemoryGroup().ToArray()[0].Span;
                return new MemoryStream(MemoryMarshal.AsBytes(pixelSpan).ToArray());
            }

            throw new ArgumentException("Not L16 png");

        }
    }
}