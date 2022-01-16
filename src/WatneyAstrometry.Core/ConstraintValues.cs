namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Constraints, values that are used in limiting parameter ranges and such.
    /// These are recommendations really and the core doesn't validate against these.
    /// These act more as guidelines for user interfaces so that they use reasonable values.
    /// </summary>
    public static class ConstraintValues
    {
        /// <summary>
        /// Maximum recommended radius (in degrees) for nearby strategy searches.
        /// Not strictly an enforced limit in core, but a recommendation.
        /// </summary>
        public const double MaxRecommendedNearbySearchRadius = 60.0;

        /// <summary>
        /// Maximum recommended value for density offsets in search strategies.
        /// Not strictly an enforced limit in core, but a recommendation.
        /// </summary>
        public const uint MaxRecommendedDensityOffset = 3;

        /// <summary>
        /// Maximum recommended value for how many stars should be used in the solving
        /// process.
        /// Not strictly an enforced limit in core, but a recommendation.
        /// </summary>
        public const uint MaxRecommendedStars = 1200;

        /// <summary>
        /// Minimum recommended field radius (in degrees) to try on blind searches.
        /// Not strictly an enforced limit in core, but a recommendation.
        /// </summary>
        public const double MinRecommendedFieldRadius = 0.1;
        
        /// <summary>
        /// Maximum recommended field radius (in degrees) to try on blind searches.
        /// Not strictly an enforced limit in core, but a recommendation.
        /// </summary>
        public const double MaxRecommendedFieldRadius = 16.0;
    }
}