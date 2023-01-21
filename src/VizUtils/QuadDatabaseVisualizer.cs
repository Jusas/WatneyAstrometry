using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.Types;

namespace VizUtils
{
    public static class QuadDatabaseVisualizer
    {
        // The how and why is here:
        // https://astronomy.stackexchange.com/questions/43449/plotting-equatorial-coordinates-to-x-y-plane-simulating-telescope-camera-view/43482#43482
        // Also: https://www.researchgate.net/publication/333841450_Astrometry_The_Foundation_for_Observational_Astronomy

        private static PointF ToImageCoords(EquatorialCoords itemCoords, EquatorialCoords imageCenterCoords,
            int imageW, int imageH, double pixelsPerRadW, double pixelsPerRadH)
        {
            double theta = 0; // CCW

            var pa = pixelsPerRadW * Math.Cos(Conversions.Deg2Rad(theta));
            var pb = pixelsPerRadH * Math.Sin(Conversions.Deg2Rad(theta));
            var pd = pixelsPerRadW * -Math.Sin(Conversions.Deg2Rad(theta));
            var pe = pixelsPerRadH * Math.Cos(Conversions.Deg2Rad(theta));
            var pc = imageW / 2.0;
            var pf = imageH / 2.0;

            var itemRaRad = Conversions.Deg2Rad(itemCoords.Ra);
            var itemDecRad = Conversions.Deg2Rad(itemCoords.Dec);
            var centerRaRad = Conversions.Deg2Rad(imageCenterCoords.Ra);
            var centerDecRad = Conversions.Deg2Rad(imageCenterCoords.Dec);

            var itemX = Math.Cos(itemDecRad) * Math.Sin(itemRaRad - centerRaRad) /
                        (Math.Cos(centerDecRad) * Math.Cos(itemDecRad) * Math.Cos(itemRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(itemDecRad));
            var itemY = (Math.Sin(centerDecRad) * Math.Cos(itemDecRad) * Math.Cos(itemRaRad - centerRaRad) - Math.Cos(centerDecRad) * Math.Sin(itemDecRad)) /
                        (Math.Cos(centerDecRad) * Math.Cos(itemDecRad) * Math.Cos(itemRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(itemDecRad));

            var pixelPointX = pa * itemX + pb * itemY + pc;
            var pixelPointY = pd * itemX + pe * itemY + pf;

            pixelPointX = imageW - pixelPointX;

            return new PointF((float)pixelPointX, (float)pixelPointY);
        }

        public static async Task<Image<Rgba32>> VisualizeAreaAndFormQuads(this CompactQuadDatabase db, Image<Rgba32> image, 
            int quadsPerSqDeg, EquatorialCoords center, double pixelSizeMm, double focalLenMm)
        {
            var degW = Conversions.Rad2Deg(2 * Math.Atan((pixelSizeMm * 1 /*bin*/ * image.Width / 1000.0) / (2 * focalLenMm)));
            var degH = Conversions.Rad2Deg(2 * Math.Atan((pixelSizeMm * 1 /*bin*/ * image.Height / 1000.0) / (2 * focalLenMm)));

            var imageWidthRad = 2 * Math.Atan((pixelSizeMm * 1 /*bin*/ * image.Width / 1000.0) / (2 * focalLenMm));
            var imageHeightRad = 2 * Math.Atan((pixelSizeMm * 1 /*bin*/ * image.Height / 1000.0) / (2 * focalLenMm));
            var pixelsPerRadW = image.Width / imageWidthRad;
            var pixelsPerRadH = image.Height / imageHeightRad;

            var solveCtx = Guid.NewGuid();
            db.CreateSolveContext(solveCtx);
            var quads = db.GetQuads(center, Math.Max(degW, degH), quadsPerSqDeg, null, 1, 0, null, solveCtx);
            db.DisposeSolveContext(solveCtx);

            foreach (var quad in quads)
            {
                var quadCenterCoords = ToImageCoords(quad.MidPoint, center, image.Width, image.Height, 
                    pixelsPerRadW, pixelsPerRadH);
                if (quadCenterCoords.Y < 0 || quadCenterCoords.Y > image.Height || quadCenterCoords.X < 0 || quadCenterCoords.X > image.Width)
                    continue;

                image.Mutate(context =>
                {
                    var color = QuadVisualizer.GenerateQuadColor(quad.MidPoint);
                    QuadVisualizer.DrawQuadSymbolTo(context, quadCenterCoords, quad, null, color,
                        $"{quad.MidPoint.ToStringRounded(3)}");
                });
            }

            return image;

        }
    }
}