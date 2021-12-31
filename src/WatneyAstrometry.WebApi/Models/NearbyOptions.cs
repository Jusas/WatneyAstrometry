using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WatneyAstrometry.WebApi.Models
{
    public class NearbyOptions
    {
        /// The search center in RA coordinate.
        [Range(0.0, 360.0)]
        [FromForm(Name = "ra")]
        public double? Ra { get; set; }
        /// The search center in Dec coordinate.
        [Range(-90.0, 90.0)]
        [FromForm(Name = "dec")]
        public double? Dec { get; set; }

        /// The (telescope) field radius (in degrees) to use.
        [Range(0.1, 30.0)]
        [FromForm(Name = "fieldRadius")]
        public double? FieldRadius { get; set; }


        /// Specifies that the assumed center and field radius is provided by the file FITS headers. Will result in an error if the input file is not FITS or it does not contain the required FITS headers.
        [FromForm(Name = "useFitsHeaders")]
        public bool? UseFitsHeaders { get; set; }
        /// The search radius (deg), the solver search will cover this area around the center coordinate.")]
        [DefaultValue(20.0)]
        [Range(0.1, 30.0)]
        [FromForm(Name = "searchRadius")]
        public double? SearchRadius { get; set; }
        
    }
}