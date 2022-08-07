// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WatneyAstrometry.Core.MathUtils;

// https://phys.libretexts.org/Bookshelves/Astronomy__Cosmology/Book%3A_Celestial_Mechanics_(Tatum)/11%3A_Photographic_Astrometry/11.02%3A_Standard_Coordinates_and_Plate_Constants
// https://www.researchgate.net/publication/333841450_Astrometry_The_Foundation_for_Observational_Astronomy
// https://afh.sonoma.edu/astrometry/

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// RA, Dec coordinates.
    /// </summary>
    public class EquatorialCoords
    {
        /// <summary>
        /// RA coordinate, degrees decimal number.
        /// </summary>
        public double Ra { get; set; }
        /// <summary>
        /// Dec coordinate, degrees decimal number.
        /// </summary>
        public double Dec { get; set; }

        /// <summary>
        /// New empty equatorialcoords.
        /// </summary>
        public EquatorialCoords()
        {
            
        }

        /// <summary>
        /// New equatorial coords from RA, Dec.
        /// </summary>
        /// <param name="ra"></param>
        /// <param name="dec"></param>
        public EquatorialCoords(double ra, double dec)
        {
            Ra = ToPositive(ra);
            Dec = dec;
        }

        /// <summary>
        /// A string representation of the coordinates.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{Ra.ToString(CultureInfo.InvariantCulture)}, {Dec.ToString(CultureInfo.InvariantCulture)}]";
        }

        /// <summary>
        /// Rounded string representation of the coordinates.
        /// </summary>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public string ToStringRounded(int decimals)
        {
            return $"[{Math.Round(Ra, decimals).ToString(CultureInfo.InvariantCulture)}, {Math.Round(Dec, decimals).ToString(CultureInfo.InvariantCulture)}]";
        }

        private static double ToPositive(double ra)
        {
            if (ra >= 0)
                return ra;

            while (ra < 0)
                ra += 360;

            return ra;
        }

        /// <summary>
        /// Gets angular distance between two points.
        /// </summary>
        public double GetAngularDistanceTo(EquatorialCoords coords)
        {
            return GetAngularDistanceBetween(this, coords);
        }

        internal static EquatorialCoords FromText(string ra, string dec)
        {
            var c = CultureInfo.InvariantCulture;
            var raHoursValue = ra; // HH MM ss
            var decDegsValue = dec; // DG MM ss

            var raElems = raHoursValue.Split(' ');
            var decElems = decDegsValue.Split(' ');

            // Because we may have '-00 xx xx'
            bool negativeSign = decElems[0].StartsWith("-");
            decElems[0] = decElems[0].Replace("-", string.Empty);

            var raH = double.Parse(raElems[0], c);
            var raM = double.Parse(raElems[1], c);
            var raS = double.Parse(raElems[2], c);
            var raInDegrees = Conversions.RaToDecimal(raH, raM, raS);

            var decD = double.Parse(decElems[0], c);
            var decM = double.Parse(decElems[1], c);
            var decS = double.Parse(decElems[2], c);
            var decInDegrees = Conversions.DecToDecimal(negativeSign, decD, decM, decS);

            return new EquatorialCoords(raInDegrees, decInDegrees);
        }

        /// <summary>
        /// Gets angular distance between two points.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>Distance between the points in degrees</returns>
        public static double GetAngularDistanceBetween(EquatorialCoords p1, EquatorialCoords p2)
        {
            var a = Math.Sin(Conversions.Deg2Rad(p1.Dec)) * Math.Sin(Conversions.Deg2Rad(p2.Dec)) +
                    Math.Cos(Conversions.Deg2Rad(p1.Dec)) * Math.Cos(Conversions.Deg2Rad(p2.Dec)) *
                    Math.Cos(Conversions.Deg2Rad(p1.Ra) - Conversions.Deg2Rad(p2.Ra));
            var angle = Math.Acos(a);
            return Conversions.Rad2Deg(angle);
        }
        

        /// <summary>
        /// Transforms the RA, Dec coords to standard coordinates around the given center.
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        public (double x, double y) ToStandardCoordinates(EquatorialCoords center)
        {
            var centerRaRad = Conversions.Deg2Rad(center.Ra);
            var centerDecRad = Conversions.Deg2Rad(center.Dec);
            var raRad = Conversions.Deg2Rad(Ra);
            var decRad = Conversions.Deg2Rad(Dec);

            var divider = (Math.Cos(centerDecRad) * Math.Cos(decRad) * Math.Cos(raRad - centerRaRad) +
                           Math.Sin(centerDecRad) * Math.Sin(decRad));

            var starX = Math.Cos(decRad) * Math.Sin(raRad - centerRaRad) / divider;
            var starY = (Math.Sin(centerDecRad) * Math.Cos(decRad) * Math.Cos(raRad - centerRaRad) - Math.Cos(centerDecRad) * Math.Sin(decRad)) /
                        divider;

            return (starX, starY);
        }

        /// <summary>
        /// Transform standard coordinates to equatorial coordinates when the field center
        /// equatorial coordinates are given.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="stdX"></param>
        /// <param name="stdY"></param>
        /// <returns></returns>
        public static EquatorialCoords StandardToEquatorial(EquatorialCoords center, double stdX, double stdY)
        {
            // TODO: add the source for the equations
            var cpRa = Conversions.Deg2Rad(center.Ra);
            var cpDec = Conversions.Deg2Rad(center.Dec);

            var ra = cpRa + Math.Atan2(-stdX, (Math.Cos(cpDec) - stdY * Math.Sin(cpDec)));
            var dec = Math.Asin(
                (Math.Sin(cpDec) + stdY * Math.Cos(cpDec)) /
                Math.Sqrt(1 + stdX * stdX + stdY * stdY));

            return new EquatorialCoords(Conversions.Rad2Deg(ra), Conversions.Rad2Deg(dec));
        }

        /// <summary>
        /// Transform the coordinates from one epoch to another by adding the precession
        /// to RA and Dec.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="jSourceEpoch"></param>
        /// <param name="jTargetEpoch"></param>
        /// <returns></returns>
        public static EquatorialCoords PrecessionTransform(EquatorialCoords coordinates, double jSourceEpoch,
            double jTargetEpoch)
        {
            // Calculations from the book: ISBN 0-935702-68-7, Explanatory Supplement to the Astronomical Almanac

            var alpha = Conversions.Deg2Rad(coordinates.Ra);
            var delta = Conversions.Deg2Rad(coordinates.Dec);

            // double julianCenturyDays = 36525;
            double j2000JulianDays = 2451545.0;

            // Source and target epoch to julian days.
            double sourceJulianDays = 365.25 * (jSourceEpoch - 2000) + j2000JulianDays;
            double targetJulianDays = 365.25 * (jTargetEpoch - 2000) + j2000JulianDays;

            // Variables as per the book.
            double T = (sourceJulianDays - j2000JulianDays) / 36525;
            double t = (targetJulianDays - sourceJulianDays) / 36525;

            // These are in seconds.
            double sigma = (2306.2181 + 1.39656 * T - 0.000139 * (T * T)) * t
                           + (0.30188 - 0.000344 * T) * (t * t)
                           + 0.017998 * (t * t * t);
            double zeta = (2306.2181 + 1.39656 * T - 0.000139 * (T * T)) * t
                          + (1.09468 + 0.000066 * T) * (t * t)
                          + 0.018203 * (t * t * t);
            double phi = (2004.3109 - 0.85330 * T - 0.000217 * (T * T)) * t
                         + (-0.42665 - 0.000217 * T) * (t * t)
                         - 0.041833 * (t * t * t);

            double sigmaRad = Conversions.Deg2Rad(sigma / 3600.0);
            double zetaRad = Conversions.Deg2Rad(zeta / 3600.0);
            double phiRad = Conversions.Deg2Rad(phi / 3600.0);

            // The angle formula is, as per the book, page 104 - 105:
            // sin(alpha - zeta)*cos(delta) = sin(alpha + sigma)*cos(delta)
            // cos(alpha - zeta)*cos(delta) = cos(alpha + sigma)*cos(phi)*cos(delta) - sin(phi)*sin(delta)
            // sin(delta) =                   cos(alpha + sigma)*sin(phi)*cos(delta) + cos(phi)*sin(delta)

            double a = Math.Sin(alpha + sigmaRad) * Math.Cos(delta);
            double b = Math.Cos(alpha + sigmaRad) * Math.Cos(phiRad) * Math.Cos(delta) - Math.Sin(phiRad) * Math.Sin(delta);
            double c = Math.Cos(alpha + sigmaRad) * Math.Sin(phiRad) * Math.Cos(delta) + Math.Cos(phiRad) * Math.Sin(delta);

            var dec = Conversions.Rad2Deg(Math.Asin(c));
            var ra = Conversions.Rad2Deg(Math.Atan2(a, b) + zetaRad);

            if (ra < 0)
                ra += 360;

            return new EquatorialCoords(ra, dec);
        }

        /// <summary>
        /// Gets the center RA, Dec of multiple coordinates.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public static EquatorialCoords GetCenterEquatorialCoords(IList<EquatorialCoords> coords)
        {
            double x = 0;
            double y = 0;
            double z = 0;

            for (var i = 0; i < coords.Count; i++)
            {
                var raRad = Conversions.Deg2Rad(coords[i].Ra);
                var decRad = Conversions.Deg2Rad(coords[i].Dec);

                x += Math.Cos(decRad) * Math.Cos(raRad);
                y += Math.Cos(decRad) * Math.Sin(raRad);
                z += Math.Sin(decRad);
            }

            x /= coords.Count;
            y /= coords.Count;
            z /= coords.Count;

            var centralRa = Math.Atan2(y, x);
            var hypotenuse = Math.Sqrt(x * x + y * y);
            var centralDec = Math.Atan2(z, hypotenuse);
            if (centralRa < 0)
                centralRa += Math.PI * 2;

            return new EquatorialCoords(Conversions.Rad2Deg(centralRa), Conversions.Rad2Deg(centralDec));
        }


        /// <summary>
        /// Project a coordinate to a simulated camera view 2d plane.
        /// </summary>
        /// <param name="coordinate">The coordinate to project</param>
        /// <param name="planeCenter">The center RA, Dec of the camera view</param>
        /// <param name="rotationDeg">Camera field rotation</param>
        /// <param name="imageWidth">Image width in pixels</param>
        /// <param name="imageHeight">Image height in pixels</param>
        /// <param name="pxSizeMicrons">Pixel size in microns</param>
        /// <param name="binning">Camera binning</param>
        /// <param name="focalLenMm">Telescope focal length in mm</param>
        /// <returns>Pixel coordinate x, y</returns>
        public static (double x, double y) ProjectToPlane(EquatorialCoords coordinate, EquatorialCoords planeCenter, double rotationDeg, int imageWidth, int imageHeight, 
            double pxSizeMicrons, int binning, double focalLenMm)
        {
            var imageWidthRad = 2 * Math.Atan((pxSizeMicrons * binning * imageWidth / 1000.0) / (2 * focalLenMm));
            var imageHeightRad = 2 * Math.Atan((pxSizeMicrons * binning * imageHeight / 1000.0) / (2 * focalLenMm));
            var pixelsPerRadW = imageWidth / imageWidthRad;
            var pixelsPerRadH = imageHeight / imageHeightRad;

            double theta = -rotationDeg; // CCW

            var pa = pixelsPerRadW * Math.Cos(Conversions.Deg2Rad(theta));
            var pb = pixelsPerRadH * Math.Sin(Conversions.Deg2Rad(theta));
            var pd = pixelsPerRadW * -Math.Sin(Conversions.Deg2Rad(theta));
            var pe = pixelsPerRadH * Math.Cos(Conversions.Deg2Rad(theta));
            var pc = imageWidth / 2.0;
            var pf = imageHeight / 2.0;

            var starRaRad = Conversions.Deg2Rad(coordinate.Ra);
            var starDecRad = Conversions.Deg2Rad(coordinate.Dec);
            var centerRaRad = Conversions.Deg2Rad(planeCenter.Ra);
            var centerDecRad = Conversions.Deg2Rad(planeCenter.Dec);

            var starX = Math.Cos(starDecRad) * Math.Sin(starRaRad - centerRaRad) /
                        (Math.Cos(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(starDecRad));
            var starY = (Math.Sin(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) - Math.Cos(centerDecRad) * Math.Sin(starDecRad)) /
                (Math.Cos(centerDecRad) * Math.Cos(starDecRad) * Math.Cos(starRaRad - centerRaRad) + Math.Sin(centerDecRad) * Math.Sin(starDecRad));

            var pixelPointX = pa * starX + pb * starY + pc;
            var pixelPointY = pd * starX + pe * starY + pf;

            pixelPointX = imageWidth - pixelPointX;

            return (pixelPointX, pixelPointY);

        }

    }
}