using System;
using System.Collections.Generic;

namespace WatneyAstrometry.Core.StarDetection
{
    /// <summary>
    /// A class representing a "bin" of star pixels, i.e. the list of a single star's pixels
    /// in an image.
    /// </summary>
    public class StarPixelBin
    {
        /// <summary>
        /// The pixels, split into rows.
        /// </summary>
        public Dictionary<int, List<StarPixel>> PixelRows = new Dictionary<int, List<StarPixel>>();

        public int Left;
        public int Right;

        public int Top;
        public int Bottom;

        /// <summary>
        /// Recalculate bounds.
        /// </summary>
        public void RecalcLeftRightTopBottom()
        {
            Left = int.MaxValue;
            Right = int.MinValue;
            Top = int.MaxValue;
            Bottom = int.MinValue;

            foreach (var row in PixelRows)
            {
                if (row.Key < Top)
                    Top = row.Key;
                if (row.Key > Bottom)
                    Bottom = row.Key;

                foreach (var p in row.Value)
                {
                    if (p.X < Left)
                        Left = p.X;
                    if (p.X > Right)
                        Right = p.X;
                }
            }
        }

        //https://www.gaia.ac.uk/sites/default/files/resources/Calculating_Magnitudes.pdf
        public (double PixelPosX, double PixelPosY, long BrightnessValue) GetCenterPixelPosAndRelativeBrightness()
        {
            // Center coordinate in small stars is generally the brightest pixel in the bin.
            // When there are more pixels of the same or almost the same brightness, we will calculate
            // their center point.

            double starPosX = 0;
            double starPosY = 0;
            var pCount = PixelCount;
            
            List<StarPixel> sortedPixels = new List<StarPixel>();
            foreach(var row in PixelRows.Keys)
            {
                sortedPixels.AddRange(PixelRows[row]);
            }

            sortedPixels.Sort((p1, p2) => p1.PixelValue < p2.PixelValue ? -1 : p1.PixelValue > p2.PixelValue ? 1 : 0);

            // With small stars just settle with the center of the canvas.
            if (pCount <= 9)
            {
                starPosY = Top + 0.5 * (Bottom - Top);
                starPosX = Left + 0.5 * (Right - Left);
                return (starPosX, starPosY, sortedPixels[sortedPixels.Count-1].PixelValue);
            }

            var l = int.MaxValue;
            var r = int.MinValue;
            var t = int.MaxValue;
            var b = int.MinValue;

            // Select 50% of the pixels ordered by brightness and just settle with the center. Probably good enough approximation.
            for (var i = (int)(Math.Ceiling(0.5 * sortedPixels.Count)); i < sortedPixels.Count; i++)
            {
                var px = sortedPixels[i];
                if (px.X < l)
                    l = px.X;
                if (px.X > r)
                    r = px.X;
                if (px.Y < t)
                    t = px.Y;
                if (px.Y > b)
                    b = px.Y;
            }

            starPosY = t + 0.5 * (b - t);
            starPosX = l + 0.5 * (r - l);
            return (starPosX, starPosY, sortedPixels[sortedPixels.Count-1].PixelValue);
        }

        public (int Width, int Height) Dimensions => (Right - Left + 1, PixelRows.Count);

        /// <summary>
        /// Count the pixels in this bin.
        /// </summary>
        public int PixelCount
        {
            get
            {
                int count = 0;
                foreach (var row in PixelRows)
                    count += row.Value.Count;
                return count;
            }
        }

        /// <summary>
        /// Create a new star pixel bin, with the initial first star pixel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public StarPixelBin(int x, int y, long value)
        {
            Left = x;
            Right = x;
            PixelRows[y] = new List<StarPixel>() {new StarPixel(x, y, value)};
        }

        /// <summary>
        /// Adds a new pixel to the bin.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public void Add(int x, int y, long value)
        {
            if (x < Left)
                Left = x;
            if (x > Right)
                Right = x;
            PixelRows[y].Add(new StarPixel(x, y, value));
        }
    }

    /// <summary>
    /// Represents a single star pixel inside the bin.
    /// </summary>
    public struct StarPixel
    {
        public int X;
        public int Y;
        public long PixelValue;

        public StarPixel(int x, int y, long value)
        {
            X = x;
            Y = y;
            PixelValue = value;
        }
    }
}