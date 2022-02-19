// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Represents a star quad.
    /// </summary>
    public class StarQuad
    {
        public float[] Ratios { get; protected set; }
        public float LargestDistance { get; protected set; }
        public EquatorialCoords MidPoint { get; protected set; }
        public virtual IReadOnlyList<IStar> Stars { get; protected set; } = new IStar[0];

        public StarQuad(float[] ratios, float largestDistance, EquatorialCoords midPoint, IList<IStar> stars = null)
        {
            //if (ratios.Length != 5)
            //    throw new Exception("Five ratios form a quad");

            Ratios = ratios;
            LargestDistance = largestDistance;
            MidPoint = midPoint;
            if (stars != null)
                Stars = stars.ToArray();
        }

        public bool IsRatioWithinThreshold(int index, float ratio, float threshold)
        {
            if (Math.Abs(Ratios[index] / ratio - 1.0f) > threshold)
                return false;
            return true;
        }

        public bool AreRatiosWithinThreshold(float[] ratios, float threshold)
        {
            float d = Math.Abs(Ratios[0] / ratios[0] - 1.0f);
            if (d > threshold) return false;
            d = Math.Abs(Ratios[1] / ratios[1] - 1.0f);
            if (d > threshold) return false;
            d = Math.Abs(Ratios[2] / ratios[2] - 1.0f);
            if (d > threshold) return false;
            d = Math.Abs(Ratios[3] / ratios[3] - 1.0f);
            if (d > threshold) return false;
            d = Math.Abs(Ratios[4] / ratios[4] - 1.0f);
            if (d > threshold) return false;
            return true;
        }

        /// <summary>
        /// For duplicate detection.
        /// </summary>
        internal class StarQuadStarBasedEqualityComparer : IEqualityComparer<StarQuad>
        {
            public bool Equals(StarQuad x, StarQuad y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Stars.All(s => y.Stars.Contains(s));
            }

            public int GetHashCode(StarQuad obj)
            {
                return obj.Stars[0].GetHashCode() ^ obj.Stars[1].GetHashCode() ^ obj.Stars[2].GetHashCode() ^
                       obj.Stars[3].GetHashCode();
            }
        }

        internal class StarQuadRatioBasedEqualityComparer : IEqualityComparer<StarQuad>
        {
            public bool Equals(StarQuad x, StarQuad y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                if (x.LargestDistance != y.LargestDistance)
                    return false;
                for (var r = 0; r < 5; r++)
                {
                    if (x.Ratios[r] != y.Ratios[r])
                        return false;
                }

                return true;
            }

            public int GetHashCode(StarQuad obj)
            {
                return obj.Ratios[0].GetHashCode() ^ obj.Ratios[1].GetHashCode() ^ obj.Ratios[2].GetHashCode() ^
                       obj.Ratios[3].GetHashCode() ^ obj.Ratios[4].GetHashCode() ^ obj.LargestDistance.GetHashCode();
            }
        }

    }
}