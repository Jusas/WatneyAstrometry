using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core.Types;

namespace VizUtils
{
    public static class QuadVisualizer
    {
        private static ShapeGraphicsOptions _lineShapeGraphicsOptions =
            new ShapeGraphicsOptions(new GraphicsOptions(), new ShapeOptions());

        public static Color GenerateQuadColor(EquatorialCoords location)
        {
            var symbolColorSeed = new Random((int)location.Ra * 1000 + (int)location.Dec * 1000);
            var symbolColor = new Argb32(
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                255);
            return symbolColor;
        }


        public static void DrawQuadSymbolTo(IImageProcessingContext context, PointF quadCenter, StarQuad quad, List<PointF> starPoints, Color color, string text)
        {

            var colors = new Dictionary<float, Color>
            {
                [0.1f] = new Argb32(234, 180, 180),
                [0.2f] = new Argb32(209, 64, 214),
                [0.3f] = new Argb32(99, 64, 214),
                [0.4f] = new Argb32(57, 140, 193),
                [0.5f] = new Argb32(46, 168, 58),
                [0.6f] = new Argb32(142, 214, 64),
                [0.7f] = new Argb32(214, 205, 64),
                [0.8f] = new Argb32(214, 159, 64),
                [0.9f] = new Argb32(176, 55, 92)
            };

            var circleRadius = 2.0f;

            var circle = new EllipsePolygon(quadCenter.X, quadCenter.Y, circleRadius+2.0f);
            var colorKey = (float)Math.Min(0.9f, Math.Max(0.1f, Math.Round(quad.Ratios[0], 1)));

            context.Fill(colors[colorKey], circle);

            for (var i = 1; i < 5; i++)
            {
                circleRadius += 2.0f;
                colorKey = (float)Math.Min(0.9f, Math.Max(0.1f, Math.Round(quad.Ratios[i], 1)));
                circle = new EllipsePolygon(quadCenter.X, quadCenter.Y, circleRadius);
                context.Draw(colors[colorKey], 3.0f, circle);
            }
            
            var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Bold);

            if (starPoints != null)
                context.DrawLines(_lineShapeGraphicsOptions, color, 1.0f, starPoints.ToArray());
            
            var glyphs = TextBuilder.GenerateGlyphs(text, new PointF(quadCenter.X, quadCenter.Y) + new PointF(4.0f, 4.0f),
                new RendererOptions(font, 72));
            context.Fill(color, glyphs);
        }

        

        public static Image<Rgba32> DrawImageQuadsFromMatches(this Image<Rgba32> baseImage, List<StarQuadMatch> matches)
        {
            foreach (var match in matches)
            {
                // With image quads we can draw lines between stars, and also a shape
                // representing the ratios.

                var points = new List<PointF>();
                foreach (var star in match.ImageStarQuad.ImageStars)
                {
                    points.Add(new PointF((float)star.X, (float)star.Y));
                }

                points.Add(new PointF(points.First().X, points.First().Y));
                var color = GenerateQuadColor(match.CatalogStarQuad.MidPoint);
                
                var quadCenter = new PointF((float) match.ImageStarQuad.PixelMidPoint.x,
                    (float) match.ImageStarQuad.PixelMidPoint.y);
                
                baseImage.Mutate(context =>
                {
                    DrawQuadSymbolTo(context, quadCenter, match.ImageStarQuad, points, color,
                        $"{Math.Round(match.ScaleRatio, 1)} | {match.CatalogStarQuad.MidPoint.ToStringRounded(3)}");
                });
            }

            return baseImage;
        }
    }
}