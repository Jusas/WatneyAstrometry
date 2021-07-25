using System;
using System.Collections.Generic;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.StarDetection;

namespace VizUtils
{
    public static class StarVisualizer
    {

        public static Image<Rgba32> VisualizeStars(this Image<Rgba32> baseImage, IList<ImageStar> detectedStars)
        {
            baseImage.Mutate(context =>
            {
                var starCircleColor = new Argb32(255, 0, 0, 100);
               
                foreach (var star in detectedStars)
                {
                    var ellipse = new EllipsePolygon((float)star.X,
                        (float)star.Y,
                        8.0f);
                    context.Draw(starCircleColor, 2.0f, ellipse);
                }
            });

            return baseImage;
        }

        public static Image<Rgba32> VisualizeStarPixelBins(this Image<Rgba32> baseImage, IList<StarPixelBin> bins)
        {
            
            foreach (var bin in bins)
            {

                var pts = new PointF[]
                {
                    new PointF((float) (bin.Left - 1), (float) (bin.Top - 1)),
                    new PointF((float) (bin.Right + 1), (float) (bin.Top - 1)),
                    new PointF((float) (bin.Right + 1), (float) (bin.Bottom + 1)),
                    new PointF((float) (bin.Left - 1), (float) (bin.Bottom + 1)),
                    new PointF((float) (bin.Left - 1), (float) (bin.Top - 1))
                };

                baseImage.Mutate(context =>
                {
                    context.DrawPolygon(Color.Red, 1.0f, pts);
                });

                var randomR = (byte)new Random().Next(40, 255);
                var randomG = (byte)new Random().Next(40, 255);
                var randomB = (byte)new Random().Next(40, 255);
                
                foreach (var linePixels in bin.PixelRows)
                {
                    for (var px = 0; px < linePixels.Value.Count; px++)
                    {
                        baseImage[linePixels.Value[px].X, linePixels.Value[px].Y] = new Rgba32(randomR, randomB, randomG);
                    }
                }
                
            }

            return baseImage;
        }

        

    }
}