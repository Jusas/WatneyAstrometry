// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.Core.StarDetection
{
    /// <summary>
    /// Default implementation of the star detection filter.
    /// <para>
    /// A star detection filter is used to filter out unwanted stars after running initial
    /// star detection. This workflow is used by the <see cref="DefaultStarDetector"/>.
    /// You may override and replace the used implementation if you wish to customize the
    /// filtering algorithms. This implementation is somewhat crude and simple.
    /// </para>
    /// </summary>
    public class DefaultStarDetectionFilter : IStarDetectionFilter
    {
        /// <summary>
        /// New filter instance.
        /// </summary>
        public DefaultStarDetectionFilter()
        {
        }

        /// <summary>
        /// Apply the filter to pixel bins, and return the filtered list of bins.
        /// </summary>
        /// <param name="starPixelBins"></param>
        /// <param name="imageMetadata"></param>
        /// <returns></returns>
        public virtual List<StarPixelBin> ApplyFilter(IReadOnlyList<StarPixelBin> starPixelBins, Metadata imageMetadata)
        {
            int tooSmall = 0;
            int tooLarge = 0;
            int notRound = 0;
            int streakish = 0;

            var accepted = new List<StarPixelBin>();
            for(var i = 0; i < starPixelBins.Count; i++)
            {
                if (IsTooSmall(starPixelBins[i]))
                    tooSmall++;
                if (IsTooLarge(starPixelBins[i], imageMetadata))
                    tooLarge++;
                if (IsNotRound(starPixelBins[i]))
                    notRound++;
                if (IsStreakish(starPixelBins[i]))
                    streakish++;

                bool add = !IsTooSmall(starPixelBins[i]) &&
                   !IsTooLarge(starPixelBins[i], imageMetadata) &&
                   !IsStreakish(starPixelBins[i]) &&
                   !IsNotRound(starPixelBins[i]);
                if(add)
                    accepted.Add(starPixelBins[i]);
            }

            // todo: additional boxing of each star, create profile, detect sharp edges and reject bad profiles.

            return accepted;
        }


        // Well, this is pretty crude but hey, it does filter out some meaningless stuff.
        /// <summary>
        /// Root out too small stars.
        /// </summary>
        /// <param name="pixelBin"></param>
        /// <returns></returns>
        protected virtual bool IsTooSmall(StarPixelBin pixelBin) => pixelBin.PixelCount < 4;

        /// <summary>
        /// Root out too large stars.
        /// </summary>
        /// <param name="pixelBin"></param>
        /// <param name="imageMetadata"></param>
        /// <returns></returns>
        protected virtual bool IsTooLarge(StarPixelBin pixelBin, Metadata imageMetadata)
        {
            // Arbitrarily chosen size, but should be reasonable.

            var maxSize = (int)(Math.Max(imageMetadata.ImageWidth, imageMetadata.ImageHeight) * 0.02);
            var dimensions = pixelBin.Dimensions;
            if (dimensions.Width > maxSize || dimensions.Height > maxSize)
                return true;

            return false;
        }

        /// <summary>
        /// Analyze if the pixel bin is streakish (star / satellite streak).
        /// </summary>
        /// <param name="pixelBin"></param>
        /// <returns></returns>
        protected virtual bool IsStreakish(StarPixelBin pixelBin)
        {
            var dimensions = pixelBin.Dimensions;
            if (dimensions.Width == 1 || dimensions.Height == 1 || (float)dimensions.Width / (float)dimensions.Height >= 6.0f ||
                (float)dimensions.Height / (float)dimensions.Width >= 6.0f)
                return true;
            return false;
        }

        /// <summary>
        /// Measures star roundness by calculating how many pixels occupy the square area
        /// the star occupies. When perfectly round, the ratio should be 1.
        /// </summary>
        /// <param name="pixelBin"></param>
        /// <returns></returns>
        protected virtual bool IsNotRound(StarPixelBin pixelBin)
        {
            var dimensions = pixelBin.Dimensions;
            var perimeter = 2 * Math.PI * 0.5 * Math.Max(dimensions.Width, dimensions.Height);
            var ratio = (4 * Math.PI * pixelBin.PixelCount) / (perimeter * perimeter);
            if (ratio < 0.25)
                return true;
            return false;
        }


    }
}