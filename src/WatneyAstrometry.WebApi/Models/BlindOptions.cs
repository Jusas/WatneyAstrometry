using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models
{
    public class BlindOptions
    {

        /// <summary>
        /// The minimum field radius (in degrees) the solver may use in search. Must be > 0.
        /// </summary>
        [Range(0.1, 30.0)]
        [DefaultValue(0.25)]
        [FromForm(Name = "minRadius")]
        public double? MinRadius { get; set; }

        /// <summary>
        /// The maximum field radius (in degrees) the solver may use in search. Must be <= 30. Search starts at max radius, and radius is divided by 2 until min-radius is reached.
        /// </summary>
        [Range(0.1, 30.0)]
        [DefaultValue(8.0)]
        [FromForm(Name = "maxRadius")]
        public double? MaxRadius { get; set; }

        /// <summary>
        /// Preferred RA sky scanning order (East or West first).
        /// East == 0..180 degrees RA.
        /// West == 180..360 degrees RA.
        /// </summary>
        [FromForm(Name = "raSearchOrder")]
        public BlindSearchStrategyOptions.RaSearchOrder? RaSearchOrder { get; set; }

        /// <summary>
        /// Preferred Dec sky scanning order (North or South first).
        /// North == 0..90 degrees Dec.
        /// South == -90..0 degrees Dec.
        /// </summary>
        [FromForm(Name = "decSearchOrder")]
        public BlindSearchStrategyOptions.DecSearchOrder? DecSearchOrder { get; set; }
       
        /// Use parallelism, search multiple areas simultaneously.
        // public bool UseParallelism { get; set; } // todo make this a setting, so that the api can force non-parallel solves
        
    }
}