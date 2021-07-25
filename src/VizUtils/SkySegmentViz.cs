using System;
using System.Collections;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WatneyAstrometry.Core.Types;

namespace VizUtils
{
    public static class SkySegmentViz
    {
        public static Image<Rgba32> DrawSkySegmentSphere(int pixelsPerDeg)
        {
            var allCells = SkySegmentSphere.Cells;
            var imageWidth = pixelsPerDeg * 360;
            var imageHeight = pixelsPerDeg * 180;

            var brush = new SolidBrush(Color.Black);
            var image = new Image<Rgba32>(imageWidth, imageHeight);
            image.Mutate(ctx =>
            {
                ctx.Fill(new GraphicsOptions(), Color.White);
                foreach (var cell in allCells)
                {
                    var pts = new PointF[]
                    {
                        new PointF((float) (cell.Bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - cell.Bounds.DecTop) * pixelsPerDeg)),
                        new PointF((float) (cell.Bounds.RaRight * pixelsPerDeg), (float) ((90.0f - cell.Bounds.DecTop) * pixelsPerDeg)),
                        new PointF((float) (cell.Bounds.RaRight * pixelsPerDeg), (float) ((90.0f - cell.Bounds.DecBottom) * pixelsPerDeg)),
                        new PointF((float) (cell.Bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - cell.Bounds.DecBottom) * pixelsPerDeg))
                    };
                    ctx.DrawPolygon(brush, 2.0f, pts);
                }
            });

            return image;

        }

        public static Image<Rgba32> DrawColoredSkySegmentCell(this Image<Rgba32> baseImage, RaDecBounds bounds, Color color)
        {
            var pixelsPerDeg = baseImage.Width / 360;
            
            var pts = new PointF[]
            {
                new PointF((float) (bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - bounds.DecTop) * pixelsPerDeg)),
                new PointF((float) (bounds.RaRight * pixelsPerDeg), (float) ((90.0f - bounds.DecTop) * pixelsPerDeg)),
                new PointF((float) (bounds.RaRight * pixelsPerDeg), (float) ((90.0f - bounds.DecBottom) * pixelsPerDeg)),
                new PointF((float) (bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - bounds.DecBottom) * pixelsPerDeg))
            };
            baseImage.Mutate(context => context.FillPolygon(color, pts));

            return baseImage;
        }

        public static Image<Rgba32> DrawColoredSkySegmentCellOutline(this Image<Rgba32> baseImage, RaDecBounds bounds, Color color)
        {
            var pixelsPerDeg = baseImage.Width / 360;
            var brush = new SolidBrush(color);

            var pts = new PointF[]
            {
                new PointF((float) (bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - bounds.DecTop) * pixelsPerDeg)),
                new PointF((float) (bounds.RaRight * pixelsPerDeg), (float) ((90.0f - bounds.DecTop) * pixelsPerDeg)),
                new PointF((float) (bounds.RaRight * pixelsPerDeg), (float) ((90.0f - bounds.DecBottom) * pixelsPerDeg)),
                new PointF((float) (bounds.RaLeft * pixelsPerDeg), (float) ((90.0f - bounds.DecBottom) * pixelsPerDeg))
            };
            baseImage.Mutate(context => context.DrawPolygon(brush, 2.0f, pts));

            return baseImage;
        }

        public static Image<Rgba32> DrawColoredEllipseOnSkySegments(this Image<Rgba32> baseImage, EquatorialCoords center,
            double radiusRa, double radiusDec, Color color)
        {
            var pixelsPerDeg = baseImage.Width / 360;
            var brush = new SolidBrush(color); //new Argb32(50, 50, 50, 100)
            float diameterRa = (float)(radiusRa * 2.0f);
            float diameterDec = (float)(radiusDec * 2.0f);

            var ellipse = new EllipsePolygon((float)(center.Ra * pixelsPerDeg), (float)((90.0f - center.Dec) * pixelsPerDeg),
                (float)(diameterRa * pixelsPerDeg), (float)(diameterDec * pixelsPerDeg));
            baseImage.Mutate(context => context.Fill(brush, ellipse));

            return baseImage;
        }

        public static Image<Rgba32> DrawColoredEllipseOutline(this Image<Rgba32> baseImage, EquatorialCoords center,
            double radiusRa, double radiusDec, Color color, float thickness)
        {
            var pixelsPerDeg = baseImage.Width / 360;
            var brush = new SolidBrush(color); //new Argb32(50, 50, 50, 100)
            float diameterRa = (float)(radiusRa * 2.0f);
            float diameterDec = (float)(radiusDec * 2.0f);

            var ellipse = new EllipsePolygon((float)(center.Ra * pixelsPerDeg), (float)((90.0f - center.Dec) * pixelsPerDeg),
                (float)(diameterRa * pixelsPerDeg), (float)(diameterDec * pixelsPerDeg));
            baseImage.Mutate(context => context.Draw(brush, thickness, ellipse));

            return baseImage;
        }

    }
}