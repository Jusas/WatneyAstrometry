﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.SolverVizTools.Abstractions;
using AvaloniaIImage = Avalonia.Media.IImage;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.Drawing
{
    public class Visualizer : IVisualizer
    {
        private readonly IServiceProvider _serviceProvider;

        private static DrawingOptions _defaultDrawingOptions = new DrawingOptions();

        public Visualizer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        private void DrawCrosshair(Image<Rgba32> baseImage)
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

        private void DrawMatchingQuads(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            try
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

                    var font = GetDefaultFont(GetQuadSymbolFontSize(solveResult));

                    points.Add(new PointF(points.First().X, points.First().Y));
                    var color = GenerateQuadColor(match.CatalogStarQuad.MidPoint);

                    var quadCenter = new PointF((float)match.ImageStarQuad.PixelMidPoint.x,
                        (float)match.ImageStarQuad.PixelMidPoint.y);

                    baseImage.Mutate(context =>
                    {
                        DrawQuadSymbolTo(context, font, quadCenter, match.ImageStarQuad, points, color,
                            $"{Math.Round(match.ScaleRatio, 1)} | {match.CatalogStarQuad.MidPoint.ToStringRounded(3)}");
                    });
                }
            }
            catch (Exception e)
            {

            }
            
        }

        private void DrawFormedQuads(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            try
            {
                var quads = solveResult.GetDiagnosticsData().FormedImageStarQuads;

                foreach (var quad in quads)
                {
                    // With image quads we can draw lines between stars, and also a shape
                    // representing the ratios.

                    var points = new List<PointF>();
                    foreach (var star in quad.ImageStars)
                    {
                        points.Add(new PointF((float)star.X, (float)star.Y));
                    }

                    var font = GetDefaultFont(GetQuadSymbolFontSize(solveResult));

                    points.Add(new PointF(points.First().X, points.First().Y));
                    var color = GenerateQuadColor((double)points.First().X, (double)points.First().Y);

                    baseImage.Mutate(context =>
                    {
                        context.DrawLines(_defaultDrawingOptions, color, 1.0f, points.ToArray());
                    });
                }
            }
            catch (Exception e)
            {

            }
            
        }


        private (Dictionary<Double, EquatorialCoords[]> raLines, Dictionary<Double, EquatorialCoords[]> decLines) GetGridLines(Solution solution)
        {

            double mainRaInterval;
            double mainDecInterval;

            var northPole = solution.EquatorialCoordsToPixel(new EquatorialCoords(0, 90));
            var southPole = solution.EquatorialCoordsToPixel(new EquatorialCoords(0, -90));

            bool northPoleVisible = northPole.x > 0 && northPole.x < solution.ImageWidth &&
                                    northPole.y > 0 && northPole.y < solution.ImageHeight;
            bool southPoleVisible = southPole.x > 0 && southPole.x < solution.ImageWidth &&
                                    southPole.y > 0 && southPole.y < solution.ImageHeight;

            if (northPoleVisible || southPoleVisible)
            {
                mainRaInterval = 30;
                mainDecInterval = 0.5;
            }
            else if (solution.Radius > 10)
            {
                mainRaInterval = 15;
                mainDecInterval = 10;
            }
            else if (solution.Radius > 5)
            {
                mainRaInterval = 5;
                mainDecInterval = 5;
            }
            else if (solution.Radius > 2)
            {
                mainRaInterval = 2;
                mainDecInterval = 2;
            }
            else if (solution.Radius > 1)
            {
                mainRaInterval = 1;
                mainDecInterval = 1;
            }
            else
            {
                mainRaInterval = 0.5;
                mainDecInterval = 0.5;
            }

            var interpolationPoints = 3;
            var decDelta = mainDecInterval / (interpolationPoints + 1);
            var raDelta = mainRaInterval / (interpolationPoints + 1);

            var raDivisions = 360 / mainRaInterval;
            var decDivisions = 180 / mainDecInterval;

            var decLinePoints = new List<EquatorialCoords>();
            var raLinePoints = new List<EquatorialCoords>();

            double r = 0;
            double d = -90;

            for (var a = 0; a < raDivisions; a++)
            {
                d = -90;
                for (var b = 0; b < decDivisions; b++)
                {
                    decLinePoints.Add(new EquatorialCoords(r, d));
                    for (var i = 1; i <= interpolationPoints; i++)
                    {
                        decLinePoints.Add(new EquatorialCoords(r, d + (i * decDelta)));
                    }

                    d += mainDecInterval;
                }
                decLinePoints.Add(new EquatorialCoords(r, 90));

                r += mainRaInterval;
            }

            
            r = 0;
            d = -90 + mainDecInterval;

            for (var a = 1; a < decDivisions; a++)
            {
                r = 0;
                for (var b = 0; b < raDivisions; b++)
                {
                    raLinePoints.Add(new EquatorialCoords(r, d));
                    for (var i = 1; i <= interpolationPoints; i++)
                    {
                        raLinePoints.Add(new EquatorialCoords(r + (i * raDelta), d));
                    }

                    r += mainRaInterval;
                }
                raLinePoints.Add(new EquatorialCoords(360, d));

                d += mainDecInterval;
            }

            var raPointsToInclude = new List<EquatorialCoords>();
            var decPointsToInclude = new List<EquatorialCoords>();

            for (var pt = 0; pt < raLinePoints.Count; pt++)
            {
                if (raLinePoints[pt].GetAngularDistanceTo(solution.PlateCenter) <= 2 * solution.Radius)
                    raPointsToInclude.Add(raLinePoints[pt]);
            }

            for (var pt = 0; pt < decLinePoints.Count; pt++)
            {
                if (decLinePoints[pt].GetAngularDistanceTo(solution.PlateCenter) <= 2 * solution.Radius)
                    decPointsToInclude.Add(decLinePoints[pt]);
            }

            var raLines = raPointsToInclude.GroupBy(x => x.Dec)
                .ToDictionary(x => x.Key, x => x.OrderBy(x => x.Ra).ToArray());
            
            var decLines = decPointsToInclude.GroupBy(x => x.Ra)
                .ToDictionary(x => x.Key, x => x.OrderBy(x => x.Dec).ToArray());

            return (raLines, decLines);

        }

        private void DrawGridLines(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            var (raLines, decLines) = GetGridLines(solveResult.Solution);

            List<PointF[]> raLinesToDraw = new List<PointF[]>();
            List<PointF[]> decLinesToDraw = new List<PointF[]>();

            baseImage.Mutate(context =>
            {
                foreach (var line in raLines)
                {
                    raLinesToDraw.Add(line.Value.Select(x =>
                    {
                        var xy = solveResult.Solution.EquatorialCoordsToPixel(x);
                        return new PointF(xy.x, xy.y);
                    }).ToArray());
                }

                foreach (var line in decLines)
                {
                    decLinesToDraw.Add(line.Value.Select(x =>
                    {
                        var xy = solveResult.Solution.EquatorialCoordsToPixel(x);
                        return new PointF(xy.x, xy.y);
                    }).ToArray());
                }

                foreach(var l in decLinesToDraw)
                    context.DrawLines(_defaultDrawingOptions, Color.FromRgba(135, 206, 235, 128), 1.0f, l);

                foreach (var l in raLinesToDraw)
                    context.DrawLines(_defaultDrawingOptions, Color.FromRgba(135, 206, 235, 128), 1.0f, l);
            });

        }

        private void DrawGridLinesOld(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            try
            {
                var gridStep = solveResult.Solution.Radius / 5;
                var c = solveResult.Solution.PlateCenter;

                for (int i = -6; i <= 6; i++)
                {
                    var dec = c.Dec - (i * gridStep);
                    var ra = c.Ra;


                }

                var lines = new List<PointF[]>();
                var center = solveResult.Solution.PlateCenter;

                // TODO determine how many lines need to be drawn by drawing until we fully reach out of bounds
                // TODO draw text (ra, dec) to the edges

                baseImage.Mutate(context =>
                {
                    var pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(0.0, 89.9999));
                    context.DrawText("P", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(0.0, 89.5));
                    context.DrawText("0", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(30.0, 89.5));
                    context.DrawText("30", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(60.0, 89.5));
                    context.DrawText("60", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(90.0, 89.5));
                    context.DrawText("90", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(120.0, 89.5));
                    context.DrawText("120", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(150.0, 89.5));
                    context.DrawText("150", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(180.0, 89.5));
                    context.DrawText("180", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(210.0, 89.5));
                    context.DrawText("210", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(240.0, 89.5));
                    context.DrawText("240", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(270.0, 89.5));
                    context.DrawText("270", GetDefaultFont(12), Color.Lime, new PointF(pos.x, pos.y));



                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(0.0, 89));
                    context.DrawText("0", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(30.0, 89));
                    context.DrawText("30", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(60.0, 89));
                    context.DrawText("60", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(90.0, 89));
                    context.DrawText("90", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(120.0, 89));
                    context.DrawText("120", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(150.0, 89));
                    context.DrawText("150", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(180.0, 89));
                    context.DrawText("180", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(210.0, 89));
                    context.DrawText("210", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(240.0, 89));
                    context.DrawText("240", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));

                    pos = solveResult.Solution.EquatorialCoordsToPixel(new EquatorialCoords(270.0, 89));
                    context.DrawText("270", GetDefaultFont(12), Color.Green, new PointF(pos.x, pos.y));
                });

                baseImage.Mutate(context =>
                {
                    var xStep = baseImage.Width / 10;
                    var yStep = baseImage.Height / 10;

                    for (var x = 0; x < 10; x++)
                    {
                        for (var y = 0; y < 10; y++)
                        {
                            var pos = new PointF(x * xStep, y * yStep);
                            var eq = solveResult.Solution.PixelToEquatorialCoords((int)pos.X, (int)pos.Y);
                            context.DrawText($"{eq.Ra:F2}, {eq.Dec:F2}", GetDefaultFont(12), Color.SkyBlue, pos);
                        }
                    }
                });
                

                baseImage.Mutate(context =>
                {
                    foreach (var line in lines)
                        context.DrawLines(_defaultDrawingOptions, Color.SkyBlue, 2.0f, line);
                });
            }
            catch (Exception e)
            {
            }
            
        }

        private void DrawDeepSkyObjects(Image<Rgba32> baseImage, SolveResult solveResult)
        {
            try
            {
                var center = solveResult.Solution.PlateCenter;
                var radius = solveResult.Solution.Radius;

                var dsoDatabase = _serviceProvider.GetService<IDsoDatabase>();
                var objects = dsoDatabase.GetInRadius(center.Ra, center.Dec, radius);

                var font = GetDefaultFont(GetDsoFontSize(solveResult));

                baseImage.Mutate(context =>
                {
                    foreach (var dso in objects)
                    {
                        var dsoPosition = solveResult.Solution.EquatorialCoordsToPixel(dso.Coords);
                        float dsoRadius = 1;
                        float dsoWidth = 1;
                        float dsoHeight = 1;
                        bool drawRotatedEllipse = false;

                        //if(dso.Size2 == 0)
                        //    dsoRadius = (float)Math.Max(1, dso.Size1); // if size2 == 0, then size1 is radius
                        //else
                        //{
                        //    dsoWidth = (float)dso.Size1; // else size is two diameters
                        //    dsoHeight = (float)dso.Size2;
                        //    drawRotatedEllipse = true;
                        //}

                        dsoRadius = (float)Math.Max(dso.Size1, dso.Size2);
                        if (dso.Size2 > 0)
                            dsoRadius *= 0.5f;

                        var ellipse = new EllipsePolygon((float)dsoPosition.x,
                            (float)dsoPosition.y,
                            dsoRadius / (float)solveResult.Solution.PixelScale);
                        context.Draw(Color.Lime, 2.0f, ellipse);

                        //if (!drawRotatedEllipse)
                        //{
                        //    var ellipse = new EllipsePolygon((float)dsoPosition.x,
                        //        (float)dsoPosition.y,
                        //        dsoRadius / (float)solveResult.Solution.PixelScale);
                        //    context.Draw(Color.Lime, 2.0f, ellipse);
                        //}
                        //else
                        //{
                        //    var ellipse = new EllipsePolygon((float)dsoPosition.x,
                        //        (float)dsoPosition.y,
                        //        dsoWidth / (float)solveResult.Solution.PixelScale,
                        //        dsoHeight / (float)solveResult.Solution.PixelScale);

                        //    var dsoCenter = new Vector2(dsoPosition.x, dsoPosition.y);
                        //    var transformedEllipse = ellipse
                        //        .Transform(Matrix3x2.CreateRotation(
                        //            (float)Conversions.Deg2Rad(solveResult.Solution.Orientation), dsoCenter))
                        //        .Transform(Matrix3x2.CreateRotation(
                        //            (float)Conversions.Deg2Rad(dso.Angle), dsoCenter));

                        //    if (solveResult.Solution.Parity == Parity.Flipped)
                        //        transformedEllipse = transformedEllipse.Transform(Matrix3x2.CreateScale(-1, 1, dsoCenter));

                        //    context.Draw(Color.Lime, 2.0f, transformedEllipse);
                        //}

                        var glyphs = TextBuilder.GenerateGlyphs(dso.DisplayName, new TextOptions(font)
                        {
                            Origin = new PointF(dsoPosition.x, dsoPosition.y) + new PointF(4.0f, 4.0f)
                        });
                        context.Fill(Color.Lime, glyphs);
                    }

                });
            }
            catch (Exception e)
            {

            }
            
        }


        private void StretchLevels(Image<Rgba32> baseImage)
        {
            baseImage.Mutate(Configuration.Default,
                new HistogramTransformation<Rgba32>(Configuration.Default, baseImage, baseImage.Bounds()));
        }

        public async Task<AvaloniaIImage> BuildVisualization(IImage sourceImage, SolveResult solveResult, VisualizationModes flags)
        {

            Avalonia.Media.Imaging.Bitmap avaloniaBitmap = null;

            await Task.Run(() =>
            {
                var img = (Image<Rgba32>)sourceImage;
                var targetImage = img.Clone();

                if (flags.HasFlag(VisualizationModes.StretchLevels))
                    StretchLevels(targetImage);

                if (flags.HasFlag(VisualizationModes.Grid))
                    DrawGridLines(targetImage, solveResult);

                if (flags.HasFlag(VisualizationModes.DetectedStars))
                    DrawDetectedStars(targetImage, solveResult);

                if (flags.HasFlag(VisualizationModes.FormedQuads))
                    DrawFormedQuads(targetImage, solveResult);

                if (flags.HasFlag(VisualizationModes.QuadMatches))
                    DrawMatchingQuads(targetImage, solveResult);

                if (flags.HasFlag(VisualizationModes.Crosshair))
                    DrawCrosshair(targetImage);

                if (flags.HasFlag(VisualizationModes.DeepSkyObjects))
                    DrawDeepSkyObjects(targetImage, solveResult);

                avaloniaBitmap = ImageConversionUtils.ImageSharpToAvaloniaBitmap(targetImage);
                targetImage.Dispose();
            });

            
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

        private static Color GenerateQuadColor(double x, double y)
        {
            var symbolColorSeed = new Random((int)x + (int)y);
            var symbolColor = new Argb32(
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                (byte)symbolColorSeed.Next(40, 255),
                255);
            return symbolColor;
        }

        private static Dictionary<int, Font> _defaultFonts = new Dictionary<int, Font>();

        private int GetDsoFontSize(SolveResult solveResult)
        {
            return (int)Math.Max(10.0, 0.01 * solveResult.GetDiagnosticsData().ImageWidth);
        }

        private int GetQuadSymbolFontSize(SolveResult solveResult)
        {
            return (int)Math.Max(10.0, 0.005 * solveResult.GetDiagnosticsData().ImageWidth);
        }

        private static Font GetDefaultFont(int size)
        {
            if (!_defaultFonts.ContainsKey(size))
            {
                try
                {
                    _defaultFonts.Add(size, SystemFonts.CreateFont("Helvetica", size, FontStyle.Bold));
                }
                catch (Exception)
                {
                }
            }
            if (!_defaultFonts.ContainsKey(size))
            {
                try
                {
                    _defaultFonts.Add(size, SystemFonts.CreateFont("Arial", size, FontStyle.Bold));
                }
                catch (Exception)
                {
                }
            }
            if (!_defaultFonts.ContainsKey(size))
            {
                try
                {
                    _defaultFonts.Add(size, SystemFonts.CreateFont("Mono", size, FontStyle.Bold));
                }
                catch (Exception)
                {
                }
            }

            if (!_defaultFonts.ContainsKey(size))
                throw new Exception("Cannot get a usable font");

            return _defaultFonts[size];
        }

        private void DrawQuadSymbolTo(IImageProcessingContext context, Font font, PointF quadCenter, StarQuad quad, 
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