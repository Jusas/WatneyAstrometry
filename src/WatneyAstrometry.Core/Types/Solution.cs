// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using WatneyAstrometry.Core.MathUtils;
#pragma warning disable CS1591

namespace WatneyAstrometry.Core.Types
{
    public enum Parity
    {
        Normal = 0,
        Flipped = 1
    }


    /// <summary>
    /// The completed astrometric solution.
    /// </summary>
    public class Solution
    {

        public class FitsHeaderFields
        {
            /// <summary>
            /// Empty constructor, for (de)serialization.
            /// </summary>
            public FitsHeaderFields()
            {
            }

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

            public double CDELT1 { get; internal set; }
            public double CDELT2 { get; internal set; }
            public double CROTA1 { get; internal set; }
            public double CROTA2 { get; internal set; }

            public double CD1_1 { get; internal set; }
            public double CD2_1 { get; internal set; }
            public double CD1_2 { get; internal set; }
            public double CD2_2 { get; internal set; }
            
            public double CRVAL1 { get; internal set; }
            public double CRVAL2 { get; internal set; }
            public double CRPIX1 { get; internal set; }
            public double CRPIX2 { get; internal set; }
        }
        /// <summary>
        /// Returns the FITS solution headers.
        /// </summary>
        public FitsHeaderFields FitsHeaders { get; internal set; }
        /// <summary>
        /// Orientation of the image, in degrees.
        /// </summary>
        public double Orientation { get; internal set; }
        /// <summary>
        /// Arc seconds per pixel.
        /// </summary>
        public double PixelScale { get; internal set; }
        /// <summary>
        /// The equatorial coords of the center of the image.
        /// </summary>
        public EquatorialCoords PlateCenter { get; internal set; }
        /// <summary>
        /// Scope or reference center point (the input expected center point, or
        /// computer chosen reference point).
        /// </summary>
        public EquatorialCoords InputCoordinates { get; internal set; }
        /// <summary>
        /// The field width, in degrees.
        /// </summary>
        public double FieldWidth { get; internal set; }
        /// <summary>
        /// The field height, in degrees.
        /// </summary>
        public double FieldHeight { get; internal set; }
        /// <summary>
        /// The field radius, in degrees.
        /// </summary>
        public double Radius { get; internal set; }
        /// <summary>
        /// Aka 'parity', whether the image is mirrored.
        /// If <see cref="Parity.Normal"/>, when North is up East is on the left.
        /// If <see cref="Parity.Flipped"/>, the image is mirrored (when North is up East is on the right).
        /// </summary>
        public Parity Parity { get; internal set; }
        /// <summary>
        /// The calculated plate constants A-F.
        /// These are used for conversions between standard, pixel and equatorial coordinates.
        /// </summary>
        public PlateConstants PlateConstants { get; internal set; }

        private readonly int _imageW;
        /// <summary>
        /// The input image width in pixels.
        /// </summary>
        public int ImageWidth => _imageW;

        private readonly int _imageH;
        /// <summary>
        /// The input image height in pixels.
        /// </summary>
        public int ImageHeight => _imageH;

        /// <summary>
        /// Empty constructor, for (de)serialization.
        /// </summary>
        public Solution()
        {
        }

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
            double crpix1, double crpix2,
            Parity parity)
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
            Parity = parity;
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