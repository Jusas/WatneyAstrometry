namespace WatneyAstrometry.Core.Tests.Utils
{
    public class ImageWriterUtils
    {
        //internal static void WriteFoundStarsImage16(List<StarPixelBin> pixelBins, FitsStarSource sourceFitsStarSource, string outputFilename)
        //{
        //    using var image = new Bitmap(sourceFitsStarSource.Width, sourceFitsStarSource.Height,
        //        PixelFormat.Format24bppRgb);
        //    for (var i = 0; i < pixelBins.Count; i++)
        //    {
        //        var bin = pixelBins[i];
        //        foreach (var linePixels in bin.PixelRows)
        //        {
        //            for (var px = 0; px < linePixels.Value.Count; px++)
        //            {
        //                var r = (i * 5 + 60) % 255;
        //                var g = (i * 10 + 60) % 255;
        //                var b = (i * 15 + 60) % 255;

        //                image.SetPixel(linePixels.Value[px].X, linePixels.Value[px].Y, Color.FromArgb(r,g,b));
        //                //image.SetPixel(linePixels.Value[px].X, linePixels.Value[px].Y, Color.White);
        //            }
        //        }

        //        //var center = bin.GetCenterPixelPosAndRelativeBrightness();
        //        //image.SetPixel((int)center.PixelPosX, (int)center.PixelPosY, Color.White);
        //    }
        //    image.Save(outputFilename, ImageFormat.Png);

        //}
    }
}