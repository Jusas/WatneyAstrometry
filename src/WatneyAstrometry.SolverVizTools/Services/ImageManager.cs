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
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.ImageReaders;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Exceptions;
using WatneyAstrometry.SolverVizTools.Models.Images;
using IImage = Avalonia.Media.IImage;
using WatneyIImage = WatneyAstrometry.Core.Image.IImage;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class ImageManager : IImageManager
    {
        // Use temporary files on disk for the drawing and for different overlays.
        // Makes things easier.

        private const string BaseImageFilename = "ui_base.png";

        public async Task<ImageData> LoadImage(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"File {filename} was not found");

            var isFits = DefaultFitsReader.IsSupported(filename);
            var imageData = new ImageData();

            if (isFits)
            {
                var reader = new DefaultFitsReader();

                await Task.Run(() =>
                {
                    imageData.WatneyImage = reader.FromFile(filename);
                    var rgbaImage = FitsImagePixelBufferToRgbaImage((FitsImage)imageData.WatneyImage);
                    imageData.EditableImage = rgbaImage;
                    imageData.SourceFormat = "FITS";

                    //var uiImageFilename = SaveAsBaseImage(rgbaImage);
                    //imageData.UiImage = new Avalonia.Media.Imaging.Bitmap(uiImageFilename);

                    imageData.UiImage = ToAvaloniaBitmap(rgbaImage);
                });

            }
            else if (CommonFormatsImageReader.IsSupported(filename))
            {
                var reader = new CommonFormatsImageReader();

                await Task.Run(() =>
                {
                    imageData.WatneyImage = reader.FromFile(filename);
                    var imageSharpImage = Image.Load(filename);
                    imageData.SourceFormat = Image.DetectFormat(filename).DefaultMimeType;

                    if (imageSharpImage is Image<Rgba32> rgbaImage)
                    {
                        //var uiImageFilename = SaveAsBaseImage(rgbaImage);
                        //imageData.UiImage = new Avalonia.Media.Imaging.Bitmap(uiImageFilename);
                        imageData.UiImage = ToAvaloniaBitmap(rgbaImage);
                    }
                    else
                    {
                        rgbaImage = imageSharpImage.CloneAs<Rgba32>();
                        //var uiImageFilename = SaveAsBaseImage(rgbaImage);
                        //imageData.UiImage = new Avalonia.Media.Imaging.Bitmap(uiImageFilename);

                        imageData.UiImage = ToAvaloniaBitmap(rgbaImage);
                    }
                });

            }
            else
            {
                throw new ImageHandlingException("Image is not in a supported format");
            }

            imageData.FileName = filename;
            imageData.Width = imageData.WatneyImage.Metadata.ImageWidth;
            imageData.Height = imageData.WatneyImage.Metadata.ImageHeight;


            return imageData;

        }

        private static string SaveAsBaseImage(Image<Rgba32> image)
        {
            var filename = Path.Combine(ProgramEnvironment.ApplicationDataFolder, BaseImageFilename); 
            image.SaveAsPng(filename, new PngEncoder() { CompressionLevel = PngCompressionLevel.NoCompression }); // ideally keep it in memory?
            return filename;
        }

        // todo save as StarOverlayImage etc


        private static unsafe Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(Image<Rgba32> image)
        {
            // RGBA == 4 bytes per pixel

            var stride = image.Width * image.PixelType.BitsPerPixel / 8;
            var pixelBuffer = new byte[image.Width * image.Height * image.PixelType.BitsPerPixel / 8];
            var stream = new MemoryStream(pixelBuffer);

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

            fixed (byte* ptr = pixelBuffer)
            {
                IntPtr p = (IntPtr)ptr;
                return new Bitmap(PixelFormat.Rgba8888, AlphaFormat.Opaque, p, new PixelSize(image.Width, image.Height),
                    new Vector(96, 96), stride);
            }
        }

        private static unsafe Image<Rgba32> FitsImagePixelBufferToRgbaImage(FitsImage fitsImage)
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

    }
}
