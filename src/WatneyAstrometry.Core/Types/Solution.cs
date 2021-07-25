using System;
using WatneyAstrometry.Core.MathUtils;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// The completed astrometric solution.
    /// </summary>
    public class Solution
    {
        public class FitsHeaderFields
        {
            public FitsHeaderFields(double cdelt1, double cdelt2, 
                double crota1, double crota2, 
                double cd1_1, double cd2_1, double cd1_2, double cd2_2, 
                double crval1, double crval2, 
                double crpix1, double crpix2)
            {
                CDELT1 = cdelt1;
                CDELT2 = cdelt2;
                CROTA1 = crota1;
                CROTA2 = crota2;
                CD1_1 = cd1_1;
                CD2_1 = cd2_1;
                CD1_2 = cd1_2;
                CD2_2 = cd2_2;
                CRVAL1 = crval1;
                CRVAL2 = crval2;
                CRPIX1 = crpix1;
                CRPIX2 = crpix2;
            }

            public double CDELT1 { get; }
            public double CDELT2 { get; }
            public double CROTA1 { get; }
            public double CROTA2 { get; }

            public double CD1_1 { get; }
            public double CD2_1 { get; }
            public double CD1_2 { get; }
            public double CD2_2 { get; }
            
            public double CRVAL1 { get; }
            public double CRVAL2 { get; }
            public double CRPIX1 { get; }
            public double CRPIX2 { get; }
        }
        /// <summary>
        /// Returns the FITS solution headers.
        /// </summary>
        public FitsHeaderFields FitsHeaders { get; }
        /// <summary>
        /// Orientation of the image, in degrees.
        /// </summary>
        public double Orientation { get; }
        /// <summary>
        /// Arc seconds per pixel.
        /// </summary>
        public double PixelScale { get; }
        /// <summary>
        /// The equatorial coords of the center of the image.
        /// </summary>
        public EquatorialCoords PlateCenter { get; }
        /// <summary>
        /// Scope or reference center point (the input expected center point, or
        /// computer chosen reference point).
        /// </summary>
        public EquatorialCoords InputCoordinates { get; }
        /// <summary>
        /// The field width, in degrees.
        /// </summary>
        public double FieldWidth { get; }
        /// <summary>
        /// The field height, in degrees.
        /// </summary>
        public double FieldHeight { get; }
        /// <summary>
        /// The field radius, in degrees.
        /// </summary>
        public double Radius { get; }
        /// <summary>
        /// The calculated plate constants A-F.
        /// These are used for conversions between standard, pixel and equatorial coordinates.
        /// </summary>
        public PlateConstants PlateConstants { get; }

        private readonly int _imageW;
        private readonly int _imageH;

        /// <summary>
        /// A successful solution.
        /// </summary>
        public Solution(EquatorialCoords inputCoordinates, EquatorialCoords imageCenter, int imageW, int imageH, double pixelScale,
            double fieldWidth, double fieldHeight, double radius,
            PlateConstants plateConstants,
            double cdelt1, double cdelt2,
            double crota1, double crota2,
            double cd1_1, double cd2_1, double cd1_2, double cd2_2,
            double crval1, double crval2,
            double crpix1, double crpix2)
        {
            FitsHeaders = new FitsHeaderFields(cdelt1, cdelt2, crota1, crota2, cd1_1, cd2_1, cd1_2, cd2_2, crval1,
                crval2, crpix1, crpix2);
            Orientation = crota1;
            PixelScale = pixelScale;
            PlateCenter = imageCenter;
            InputCoordinates = inputCoordinates;
            FieldWidth = fieldWidth;
            FieldHeight = fieldHeight;
            Radius = radius;
            PlateConstants = plateConstants;

            _imageH = imageH;
            _imageW = imageW;
        }
        

        /// <summary>
        /// Returns the equatorial coordinates of a specific pixel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public EquatorialCoords PixelToEquatorialCoords(int x, int y)
        {
            var centerX = _imageW / 2;
            var centerY = _imageH / 2;

            var pxDx = x - centerX;
            var pxDy = y - centerY;
            var dRaRad = Conversions.Deg2Rad(FitsHeaders.CD1_1 * pxDx + FitsHeaders.CD1_2 * pxDy);
            var dDecRad = Conversions.Deg2Rad(FitsHeaders.CD2_1 * pxDx + FitsHeaders.CD2_2 * pxDy);
            var refDecRad = Conversions.Deg2Rad(PlateCenter.Dec);

            var d = Math.Cos(refDecRad) - dDecRad * Math.Sin(refDecRad);
            var g = Math.Sqrt(dRaRad * dRaRad + d * d);

            var pixelRa = PlateCenter.Ra + Conversions.Rad2Deg(Math.Atan2(dRaRad, d));
            var pixelDec = Conversions.Rad2Deg(Math.Atan((Math.Sin(refDecRad) + dDecRad * Math.Cos(refDecRad)) / g));

            return new EquatorialCoords(pixelRa, pixelDec);
        }
        
        /// <summary>
        /// Returns the pixel position of the specified equatorial coordinates, or null
        /// if the pixel is outside the image.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public (int x, int y)? EquatorialCoordsToPixel(EquatorialCoords coords)
        {
            var stdc = InputCoordinates.ToStandardCoordinates(coords);
            var pc = PlateConstants;

            var y = (-(-pc.A / pc.D * pc.F) + -pc.C + (-pc.A / pc.D * stdc.y) + stdc.x) /
                    (pc.B + -pc.A / pc.D * pc.E);
            var x = (-pc.B * y + (-pc.C) + stdc.x) / pc.A;

            (int, int)? returnValue = null;
            if (x > 0 && x <= _imageW && y > 0 && y <= _imageH)
                returnValue = ((int) Math.Round(x), (int) Math.Round(y));

            return returnValue;
            
        }

    }
}