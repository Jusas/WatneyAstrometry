// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Represents a star quad (in database or in image).
    /// </summary>
    public class StarQuad
    {
        /// <summary>
        /// The quad ratios (between stars).
        /// </summary>
        public float[] Ratios { get; protected set; }
        /// <summary>
        /// The largest distance (degrees or pixels).
        /// </summary>
        public float LargestDistance { get; protected set; }
        /// <summary>
        /// The mid point of the quad.
        /// </summary>
        public EquatorialCoords MidPoint { get; protected set; }
        /// <summary>
        /// The stars that make up this quad.
        /// </summary>
        public virtual IReadOnlyList<IStar> Stars { get; protected set; } = new IStar[0];

        /// <summary>
        /// New quad from known ratios, largest distance, midpoint and stars.
        /// </summary>
        /// <param name="ratios"></param>
        /// <param name="largestDistance"></param>
        /// <param name="midPoint"></param>
        /// <param name="stars"></param>
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