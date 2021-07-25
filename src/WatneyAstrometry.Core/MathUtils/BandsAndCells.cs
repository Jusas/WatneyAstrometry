// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.MathUtils
{
    public class BandsAndCells
    {

        private static double AngleDiff(double angle1, double angle2)
        {
            double diff = (angle1 - angle2 + 180) % 360 - 180;
            return Math.Abs(diff < -180 ? diff + 360 : diff);
        }

        /// <summary>
        /// Checks if a <see cref="Cell"/> is within the given search radius from the given equatorial coordinates.
        /// </summary>
        /// <param name="radiusDeg">The search radius.</param>
        /// <param name="searchCenter">The coordinate.</param>
        /// <param name="cellBounds">The Cell's RA, Dec bounds.</param>
        /// <returns>True if within radius, otherwise false</returns>
        public static bool IsCellInSearchRadius(double radiusDeg, EquatorialCoords searchCenter,
            RaDecBounds cellBounds)
        {
            // Can short out if the Cell Dec range is not in between searchCenter.Dec +/- radius.

            var searchTopDec = searchCenter.Dec + radiusDeg;
            var searchBottomDec = searchCenter.Dec - radiusDeg;

            if (cellBounds.DecTop < searchBottomDec || cellBounds.DecBottom > searchTopDec)
                return false;
            
            var cellDecNearestPoint = Math.Max(cellBounds.DecBottom, Math.Min(searchCenter.Dec, cellBounds.DecTop));

            // A bit hacky, but if we're at the pole, any cell that is at the pole is naturally in range.
            if (searchTopDec >= 90 && cellBounds.DecTop == 90)
                cellDecNearestPoint = 90;
            

            double cellRaNearestPoint;
            if (searchCenter.Ra > cellBounds.RaLeft && searchCenter.Ra < cellBounds.RaRight)
                cellRaNearestPoint = searchCenter.Ra;
            else
            {
                var leftDiff = AngleDiff(searchCenter.Ra, cellBounds.RaLeft);
                var rightDiff = AngleDiff(searchCenter.Ra, cellBounds.RaRight);
                cellRaNearestPoint = leftDiff < rightDiff ? cellBounds.RaLeft : cellBounds.RaRight;
            }
            

            var cellNearestPoint = new EquatorialCoords(cellRaNearestPoint, cellDecNearestPoint);
            return cellNearestPoint.Ra == searchCenter.Ra && cellNearestPoint.Dec == searchCenter.Dec 
                ? true 
                : (radiusDeg - EquatorialCoords.GetAngularDistanceBetween(searchCenter, cellNearestPoint)) > 0;

        }
        
    }
}