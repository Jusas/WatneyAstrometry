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
using WatneyAstrometry.SolverVizTools.Drawing;
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
                    var rgbaImage = ImageConversionUtils.FitsImagePixelBufferToRgbaImage((FitsImage)imageData.WatneyImage);
                    imageData.EditableImage = rgbaImage;
                    imageData.SourceFormat = "FITS";
                    imageData.UiImage = ImageConversionUtils.ImageSharpToAvaloniaBitmap(rgbaImage);
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
                        imageData.UiImage = ImageConversionUtils.ImageSharpToAvaloniaBitmap(rgbaImage);
                        imageData.EditableImage = rgbaImage;
                    }
                    else
                    {
                        rgbaImage = imageSharpImage.CloneAs<Rgba32>();
                        imageData.UiImage = ImageConversionUtils.ImageSharpToAvaloniaBitmap(rgbaImage);
                        imageData.EditableImage = rgbaImage;
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
        

    }
}
