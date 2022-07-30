using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.SolverVizTools.Abstractions;
using AvaloniaIImage = Avalonia.Media.IImage;

namespace WatneyAstrometry.SolverVizTools.Drawing
{
    public class Visualizer : IVisualizer
    {

        private static DrawingOptions _defaultDrawingOptions = new DrawingOptions();
        private static void DrawCrosshair(Image<Rgba32> baseImage)
        {
            var width = (baseImage.Width - 1);
            var height = (baseImage.Height - 1);
            var lines = new List<PointF[]>()
            {
                new PointF[]
                {
                    new PointF(0.5f * width, 0),
                    new PointF(0.5f * width, height)
                },
                new PointF[]
                {
                    new PointF(0, 0.5f * height),
                    new PointF(width, 0.5f * height)
                }
            };
            baseImage.Mutate(context =>
            {
                context.DrawLines(_defaultDrawingOptions, Color.Beige, 2.0f, lines[0]);
                context.DrawLines(_defaultDrawingOptions, Color.Beige, 2.0f, lines[1]);
            });
        }

        private static void DrawDetectedStars(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            var stars = solveResult.GetDiagnosticsData().DetectedStars;
            baseImage.Mutate(context =>
            {
                var starCircleColor = new Argb32(255, 0, 0, 180);

                foreach (var star in stars)
                {
                    var ellipse = new EllipsePolygon((float)star.X,
                        (float)star.Y,
                        8.0f);
                    context.Draw(starCircleColor, 2.0f, ellipse);
                }
            });
        }

        private static void DrawQuads(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            var matches = solveResult.GetDiagnosticsData().MatchInstances;

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

                var quadCenter = new PointF((float)match.ImageStarQuad.PixelMidPoint.x,
                    (float)match.ImageStarQuad.PixelMidPoint.y);

                baseImage.Mutate(context =>
                {
                    DrawQuadSymbolTo(context, quadCenter, match.ImageStarQuad, points, color,
                        $"{Math.Round(match.ScaleRatio, 1)} | {match.CatalogStarQuad.MidPoint.ToStringRounded(3)}");
                });
            }
        }

        private static void DrawGridLines(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            var gridStep = solveResult.Solution.Radius * 2 / 10;
            var lines = new List<PointF[]>();
            var center = solveResult.Solution.PlateCenter;

            // TODO determine how many lines need to be drawn by drawing until we fully reach out of bounds
            // TODO draw text (ra, dec) to the edges

            for (var i = -10; i < 10; i++)
            {
                var coordPair = new(int x, int y)[]
                {
                    solveResult.Solution.EquatorialCoordsToPixel(
                        new EquatorialCoords(center.Ra + i * gridStep, center.Dec - 1.5 * solveResult.Solution.Radius)),
                    solveResult.Solution.EquatorialCoordsToPixel(
                        new EquatorialCoords(center.Ra + i * gridStep, center.Dec + 1.5 * solveResult.Solution.Radius))
                };
                lines.Add(new PointF[]
                {
                    new PointF(coordPair[0].x, coordPair[0].y),
                    new PointF(coordPair[1].x, coordPair[1].y)
                });
            }

            for (var i = -10; i < 10; i++)
            {
                var coordPair = new (int x, int y)[]
                {
                    solveResult.Solution.EquatorialCoordsToPixel(
                        new EquatorialCoords(center.Ra - 1.5 * solveResult.Solution.Radius, center.Dec + i * gridStep)),
                    solveResult.Solution.EquatorialCoordsToPixel(
                        new EquatorialCoords(center.Ra + 1.5 * solveResult.Solution.Radius, center.Dec + i * gridStep))
                };
                lines.Add(new PointF[]
                {
                    new PointF(coordPair[0].x, coordPair[0].y),
                    new PointF(coordPair[1].x, coordPair[1].y)
                });
            }

            baseImage.Mutate(context =>
            {
                foreach(var line in lines)
                    context.DrawLines(_defaultDrawingOptions, Color.SkyBlue, 2.0f, line);
            });
        }


        public async Task<AvaloniaIImage> BuildVisualization(IImage sourceImage, SolveResult solveResult, VisualizationModes flags)
        {
            var img = (Image<Rgba32>)sourceImage;
            var targetImage = img.Clone();

            if(flags.HasFlag(VisualizationModes.Grid))
                DrawGridLines(targetImage, solveResult);

            if(flags.HasFlag(VisualizationModes.DetectedStars))
                DrawDetectedStars(targetImage, solveResult);

            if(flags.HasFlag(VisualizationModes.Quads))
                DrawQuads(targetImage, solveResult);

            if (flags.HasFlag(VisualizationModes.Crosshair))
                DrawCrosshair(targetImage);

            var avaloniaBitmap = ImageConversionUtils.ImageSharpToAvaloniaBitmap(targetImage);
            targetImage.Dispose();
            return avaloniaBitmap;
        }

        private static Color GenerateQuadColor(EquatorialCoords location)
        {
            var symbolColorSeed = new Random((int)location.Ra * 1000 + (int)location.Dec * 1000);
            var symbolColor = new Argb32(
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                255);
            return symbolColor;
        }

        private static Font _defaultFont;

        private static Font GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                try
                {
                    _defaultFont = SystemFonts.CreateFont("Helvetica", 10, FontStyle.Bold);
                }
                catch (Exception)
                {
                }
            }
            if (_defaultFont == null)
            {
                try
                {
                    _defaultFont = SystemFonts.CreateFont("Arial", 10, FontStyle.Bold);
                }
                catch (Exception)
                {
                }
            }
            if (_defaultFont == null)
            {
                try
                {
                    _defaultFont = SystemFonts.CreateFont("Mono", 10, FontStyle.Bold);
                }
                catch (Exception)
                {
                }
            }

            if (_defaultFont == null)
                throw new Exception("Cannot get a usable font");

            return _defaultFont;
        }

        private static void DrawQuadSymbolTo(IImageProcessingContext context, PointF quadCenter, StarQuad quad, 
            List<PointF> starPoints, Color color, string text)
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

            var circle = new EllipsePolygon(quadCenter.X, quadCenter.Y, circleRadius + 2.0f);
            var colorKey = (float)Math.Min(0.9f, Math.Max(0.1f, Math.Round(quad.Ratios[0], 1)));

            context.Fill(colors[colorKey], circle);

            for (var i = 1; i < 5; i++)
            {
                circleRadius += 2.0f;
                colorKey = (float)Math.Min(0.9f, Math.Max(0.1f, Math.Round(quad.Ratios[i], 1)));
                circle = new EllipsePolygon(quadCenter.X, quadCenter.Y, circleRadius);
                context.Draw(colors[colorKey], 3.0f, circle);
            }

            var font = GetDefaultFont();
            
            if (starPoints != null)
                context.DrawLines(_defaultDrawingOptions, color, 1.0f, starPoints.ToArray());

            //var glyphs = TextBuilder.GenerateGlyphs(text, new PointF(quadCenter.X, quadCenter.Y) + new PointF(4.0f, 4.0f),
            //    new RendererOptions(font, 72));
            var glyphs = TextBuilder.GenerateGlyphs(text, new TextOptions(font)
            {
                Origin = new PointF(quadCenter.X, quadCenter.Y) + new PointF(4.0f, 4.0f)
            });
            context.Fill(color, glyphs);
        }
    }
}
