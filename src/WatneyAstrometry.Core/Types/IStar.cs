// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Interface for a star (in image or otherwise).
    /// </summary>
    public interface IStar
    {
        /// <summary>
        /// Calculate the distance between two stars.
        /// </summary>
        /// <param name="anotherStar"></param>
        /// <returns></returns>
        double CalculateDistance(IStar anotherStar);
    }
}