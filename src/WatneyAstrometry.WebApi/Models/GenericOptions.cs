

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WatneyAstrometry.WebApi.Models
{
    public abstract class GenericOptions
    {
        /// Maximum number of stars to use from the image. When not given, the solver decides itself.
        /// When given, the solver uses this number. In cases of very high star count present in the image (wide-field images), the solve may fail if this number is not set high enough.
        /// A low number of stars will speed up the solve, since it means less calculations are required, but there's a bigger chance that the solve will fail. 300 is generally a good value.
        /// A high number (> 800) would however also affect performance due to the high number of calculations, and this gets especially noticeable with blind solves.
        /// The hard limit is set to 800.
        [Range(0, 800)]
        public int MaxStars { get; set; }

        /// Try to solve the field using a sampled set of database quads first. With sampling, we try to match
        /// the image's star quads to only a fraction of the available database quads at a time, effectively making the search faster. The idea is that even if we can't find
        /// a solution (enough matching quads), we still get potential matching areas with one or more matching quad, which we can then scan with a full set of database quads
        /// to get the answer faster. Less work is performed in scanning, which makes it faster. Recommended (and default) value to use is 4 but some images may well solve
        /// faster with higher values. Too high values will however result in time wasted in scanning and making the solve actually slower.
        [Range(0, 16)]
        public int Sampling { get; set; }

        /// Include this many lower quad density passes in search (compared to image quad density).
        /// For practical purposes this is limited to 0 .. 3.
        [Range(0, 3)]
        [DefaultValue((uint)1)]
        public uint LowerDensityOffset { get; set; }

        /// Include this many higher quad density passes in search (compared to image quad density).
        /// For practical purposes this is limited to 0 .. 3.
        [Range(0, 3)]
        [DefaultValue((uint)1)]
        public uint HigherDensityOffset { get; set; }
        

    }
}