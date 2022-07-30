using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using WatneyAstrometry.Core.Fits;

namespace WatneyAstrometry.SolverVizTools.Drawing
{
    public static class ImageConversionUtils
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

        public static unsafe Avalonia.Media.Imaging.Bitmap ImageSharpToAvaloniaBitmap(Image<Rgba32> image)
        {
            // RGBA == 4 bytes per pixel

            var stride = image.Width * image.PixelType.BitsPerPixel / 8;
            var pixelBuffer = new byte[image.Width * image.Height * image.PixelType.BitsPerPixel / 8];
            var stream = new MemoryStream(pixelBuffer);

            if (image.DangerousTryGetSinglePixelMemory(out var pixels))
            {
                var buf = MemoryMarshal.AsBytes(pixels.Span).ToArray();
                stream.Write(buf, 0, buf.Length);
            }
            else
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pixelRowSpan = image.DangerousGetPixelRowMemory(y);
                    var buf = MemoryMarshal.AsBytes(pixelRowSpan.Span).ToArray();
                    stream.Write(buf, 0, buf.Length);
                }
            }

            fixed (byte* ptr = pixelBuffer)
            {
                IntPtr p = (IntPtr)ptr;
                return new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Opaque, p, new PixelSize(image.Width, image.Height),
                    new Vector(96, 96), stride);
            }
        }
    }
}
