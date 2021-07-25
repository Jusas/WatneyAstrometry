//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.Drawing;
//using SixLabors.ImageSharp.Drawing.Processing;
//using SixLabors.ImageSharp.PixelFormats;
//using SixLabors.ImageSharp.Processing;
//using WatneyAstrometry.Core;
//using WatneyAstrometry.Core.MathUtils;
//using WatneyAstrometry.Core.QuadDb;
//using WatneyAstrometry.Core.Types;

//namespace VizUtils
//{
//    public static class StarDbVisualizer
//    {
//        public static async Task VisualizeAreaAndFormQuads(this CompactQuadDatabase db, EquatorialCoords center, int width, int height, double pixelSizeMm, int bin,
//            double focalLenMm, int maxNumStars, string outputFilename, bool starDots, bool quadLines)
//        {
//            var degW = Conversions.Rad2Deg(2 * Math.Atan((pixelSizeMm * bin * width / 1000.0) / (2 * focalLenMm)));
//            var degH = Conversions.Rad2Deg(2 * Math.Atan((pixelSizeMm * bin * height / 1000.0) / (2 * focalLenMm)));

//            var imageWidthRad = 2 * Math.Atan((pixelSizeMm * bin * width / 1000.0) / (2 * focalLenMm));
//            var imageHeightRad = 2 * Math.Atan((pixelSizeMm * bin * height / 1000.0) / (2 * focalLenMm));
//            var pixelsPerRadW = width / imageWidthRad;
//            var pixelsPerRadH = height / imageHeightRad;

//            var stars = await db.GetStarsAsync(center, Math.Max(degW, degH), maxNumStars);
            
//            var quadsFromCatalogStars = Solver.FormQuads(stars);

//            (double x, double y) __ToImageCoords(CatalogStar catalogStar)
//            {
                
//                double theta = -268; // CCW
                
//                var pa = pixelsPerRadW * Math.Cos(Conversions.Deg2Rad(theta));
//                var pb = pixelsPerRadH * Math.Sin(Conversions.Deg2Rad(theta));
//                var pd = pixelsPerRadW * -Math.Sin(Conversions.Deg2Rad(theta));
//                var pe = pixelsPerRadH * Math.Cos(Conversions.Deg2Rad(theta));
//                var pc = width / 2.0;
//                var pf = height / 2.0;

//                var starRaRad = Conversions.Deg2Rad(catalogStar.Ra);
//                var starDecRad = Conversions.Deg2Rad(catalogStar.Dec);
//                var centerRaRad = Conversions.Deg2Rad(center.Ra);
//                var centerDecRad = Conversions.Deg2Rad(center.Dec);

//                var starX = Math.Cos(starDecRad) * Math.Sin(starRaRad - centerRaRad) /
//                            (Math.Cos(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(starDecRad));
//                var starY = (Math.Sin(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) - Math.Cos(centerDecRad) * Math.Sin(starDecRad)) /
//                    (Math.Cos(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(starDecRad));

//                var pixelPointX = pa * starX + pb * starY + pc;
//                var pixelPointY = pd * starX + pe * starY + pf;

//                pixelPointX = width - pixelPointX;

//                return (pixelPointX, pixelPointY);
                
//            }

//            var imageSpaceQuads = new List<ImageStarQuad>();
//            foreach (var quad in quadsFromCatalogStars)
//            {
//                var convertedStars = quad.Stars.Cast<CatalogStar>()
//                    .Select(
//                        x =>
//                        {
//                            var imageCoords = __ToImageCoords(x);
//                            return new ImageStar(imageCoords.x, imageCoords.y, 1);
//                        });
//                if (convertedStars.Any(x => x.Y < 0 || x.Y > height || x.X < 0 || x.X > width))
//                    continue;

//                var imageSpaceQuad = new ImageStarQuad(quad.Ratios, quad.LargestDistance, convertedStars.ToList());
//                imageSpaceQuads.Add(imageSpaceQuad);
//            }

//            List<ImageStar> imageStars = new List<ImageStar>();
//            foreach (var star in stars)
//            {

//                var ic = __ToImageCoords(star);
                
//                if (ic.x > 0 && ic.x < width && ic.y > 0 && ic.y < height)
//                {
//                    imageStars.Add(new ImageStar(ic.x, ic.y, MagToL(star.Mag)));
//                }
//            }

//            using (var image = new Image<Rgba32>(width, height))
//            {
//                image.Mutate(ctx =>
//                {
//                    ctx.Fill(new GraphicsOptions(), Color.Black);
                    
//                    float crossLen = 3;
//                    if (starDots)
//                    {
//                        foreach (var star in imageStars)
//                        {
//                            var l = (byte)star.Brightness;
//                            var color = new Color(new Rgb24(l, l, l));
//                            var ellipse = new EllipsePolygon((float)star.X, (float)star.Y,
//                                9.0f, 9.0f);
//                            ctx.Fill(color, ellipse);
//                        }
//                    }

//                    if (quadLines)
//                    {
//                        foreach (var quad in imageSpaceQuads)
//                        {
//                            var points = new List<PointF>();
//                            foreach (var star in quad.Stars.Cast<ImageStar>())
//                            {
//                                points.Add(new PointF((float)star.X, (float)star.Y));
//                            }

//                            points.Add(new PointF(points.First().X, points.First().Y));
//                            var randomR = (byte)new Random().Next(40, 255);
//                            var randomG = (byte)new Random().Next(40, 255);
//                            var randomB = (byte)new Random().Next(40, 255);

//                            ctx.DrawLines(new ShapeGraphicsOptions(), Color.FromRgb(randomR, randomG, randomB), 1.0f,
//                                points.ToArray());

//                            var ellipse = new EllipsePolygon((float)quad.MidPoint.x, (float)quad.MidPoint.y,
//                                5.0f, 5.0f);
//                            ctx.Fill(Color.Teal, ellipse);
                            
//                        }
//                    }
                    

//                });
                
//                image.SaveAsPng(outputFilename);

//            }


//        }

//        private static byte MagToL(float mag)
//        {
//            mag = Math.Max(0, mag);
//            var l = Math.Max(50.0f, 255.0f - (mag * 10.0f));
//            return (byte) l;
//        }
//    }
//}