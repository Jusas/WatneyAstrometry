// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// An unnamed star in equatorial coordinates.
    /// </summary>
    [Obsolete("No more catalog star needed, since we're using a quad database")]
    public class CatalogStar : IStar
    {
        public double Ra { get; private set; }
        public double Dec { get; private set; }
        public float Mag { get; private set; }

        private static byte[] _buf = new byte[sizeof(double) * 2 + sizeof(float)];

        public CatalogStar()
        {
        }

        public CatalogStar(double ra, double dec, float mag)
        {
            Ra = ra;
            Dec = dec;
            Mag = mag;
        }
        
        //public byte[] Serialize()
        //{
        //    var raBytes = BitConverter.GetBytes(Ra);
        //    var decBytes = BitConverter.GetBytes(Dec);
        //    var magBytes = BitConverter.GetBytes(Mag);
            
        //    return raBytes
        //        .Concat(decBytes)
        //        .Concat(magBytes)
        //        .ToArray();
        //}
        
        //public static CatalogStar Deserialize(byte[] serialized)
        //{
        //    var star = new CatalogStar
        //    {
        //        Ra = BitConverter.ToDouble(serialized, 0),
        //        Dec = BitConverter.ToDouble(serialized, sizeof(double)),
        //        Mag = BitConverter.ToSingle(serialized, 2 * sizeof(double))
        //    };
            
        //    return star;
        //}

        //public static CatalogStar FromStream(Stream stream)
        //{
        //    //var buf = new byte[sizeof(double) * 2 + sizeof(float)];
        //    var bytesRead = stream.Read(_buf, 0, _buf.Length);
        //    if (bytesRead < _buf.Length)
        //        return null;

        //    return Deserialize(_buf);
        //}

        /// <summary>
        /// Gets angular distance between this and another CatalogStar.
        /// </summary>
        /// <param name="anotherStar"></param>
        /// <returns></returns>
        public double CalculateDistance(IStar anotherStar)
        {
            CatalogStar s = (CatalogStar) anotherStar;
            return EquatorialCoords.GetAngularDistanceBetween(this, s);
        }
        
        /// <summary>
        /// For duplicate detection.
        /// </summary>
        internal class CoordinateEqualityComparer : IEqualityComparer<CatalogStar>
        {
            public bool Equals(CatalogStar x, CatalogStar y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Dec == y.Dec && x.Ra == y.Ra;
            }

            public int GetHashCode(CatalogStar obj)
            {
                return obj.Dec.GetHashCode() ^ obj.Ra.GetHashCode();
            }
        }

        /// <summary>
        /// Projects the equatorial coordinates via conversion to standard coordinates to a simulated image plane.
        /// </summary>
        /// <param name="planeCenter">Image plane center coordinates</param>
        /// <param name="rotationDeg">Image field rotation, in degrees</param>
        /// <param name="imageWidth">Image width, in pixels</param>
        /// <param name="imageHeight">Image height, in pixels</param>
        /// <param name="pxSizeMicrons">Pixel size in microns</param>
        /// <param name="binning">Vertical and horizontal binning</param>
        /// <param name="focalLenMm">Camera/telescope focal length, in mm</param>
        /// <returns></returns>
        public (double x, double y) ProjectToPlane(EquatorialCoords planeCenter, double rotationDeg, int imageWidth, int imageHeight, double pxSizeMicrons, int binning,
            double focalLenMm)
        {
            var coords = new EquatorialCoords(Ra, Dec);
            return EquatorialCoords.ProjectToPlane(coords, planeCenter, rotationDeg, imageWidth, imageHeight, pxSizeMicrons,
                binning, focalLenMm);
        }

    }
}