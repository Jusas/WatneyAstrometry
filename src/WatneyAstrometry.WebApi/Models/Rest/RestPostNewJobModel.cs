// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models.Rest
{
    /// <summary>
    /// New solver job data model.
    /// </summary>
    public class RestPostNewJobModel
    {
        /// <summary>
        /// The job input parameters.
        /// </summary>
        [FromForm(Name = "args")]
        public RestJobParametersModel Parameters { get; set; }
        
        internal IFormFile Image { get; set; }

        /// <summary>
        /// Perform validation on the job parameters after they've passed the first asp.net validation.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string[]> Validate()
        {
            var errors = new Dictionary<string, string[]>();

            string[] arr(string item) => new string[] { item };

            if(Image == null)
                errors.Add("image", arr("Image was not defined."));

            else if(Image?.Length == 0)
               errors.Add("image", arr("Image size is zero bytes."));

            var fileExtension = Path.GetExtension(Image.FileName).ToLowerInvariant();
            var supportedImageExtensions = new[] { ".fit", ".fits", ".png", ".jpg", ".jpeg" };
            if(supportedImageExtensions.All(e => e != fileExtension))
                errors.Add("image", arr($"Unsupported file extension. Supported file extensions are: {string.Join(", ", supportedImageExtensions)}."));

            if("nearby" != Parameters.Mode && "blind" != Parameters.Mode)
                errors.Add("mode", arr("Mode must be set to either blind or nearby"));

            if (Parameters.Mode == "nearby")
            {
                var useFitsHeaders = Parameters.NearbyParameters?.UseFitsHeaders ?? false;
                var p = Parameters.NearbyParameters;

                if (p.SearchRadius == null)
                    errors.Add("searchRadius", arr("searchRadius must be set for nearby solves"));
                else if(p.SearchRadius.Value > ConstraintValues.MaxRecommendedNearbySearchRadius)
                    errors.Add("searchRadius", arr($"searchRadius should be <= {ConstraintValues.MaxRecommendedNearbySearchRadius}"));

                if (!useFitsHeaders)
                {
                    if((p?.Ra == null || p.Dec == null || p.MaxFieldRadius == null || p.MinFieldRadius == null))
                        errors.Add("useFitsHeaders", arr($"ra, dec, minRadius and maxRadius must be provided when useFitsHeaders is false"));
                    
                    if(p.MaxFieldRadius != null && p.MaxFieldRadius > ConstraintValues.MaxRecommendedFieldRadius)
                        errors.Add("maxRadius", arr($"maxRadius should be <= {ConstraintValues.MaxRecommendedFieldRadius}"));

                    if (p.MinFieldRadius != null && p.MinFieldRadius < ConstraintValues.MinRecommendedFieldRadius)
                        errors.Add("minRadius", arr($"minRadius should be >= {ConstraintValues.MinRecommendedFieldRadius}"));
                }
            }

            if (Parameters.Mode == "blind")
            {
                var p = Parameters.BlindParameters;
                if (p?.MaxRadius == null)
                    errors.Add("maxRadius", arr("maxRadius must be set when solve mode is blind"));
                else if(p.MaxRadius.Value > ConstraintValues.MaxRecommendedFieldRadius)
                    errors.Add("maxRadius", arr($"maxRadius should be <= {ConstraintValues.MaxRecommendedFieldRadius}"));
                if (p?.MinRadius == null)
                    errors.Add("minRadius", arr("minRadius must be set when solve mode is blind"));
                else if(p.MinRadius < ConstraintValues.MinRecommendedFieldRadius)
                    errors.Add("minRadius", arr($"minRadius should be >= {ConstraintValues.MinRecommendedFieldRadius}"));

            }

            if(Parameters.HigherDensityOffset > ConstraintValues.MaxRecommendedDensityOffset)
                errors.Add("higherDensityOffset", arr($"higherDensityOffset should be kept <= {ConstraintValues.MaxRecommendedDensityOffset} for performance reasons"));
            if (Parameters.LowerDensityOffset > ConstraintValues.MaxRecommendedDensityOffset)
                errors.Add("lowerDensityOffset", arr($"lowerDensityOffset should be kept <= {ConstraintValues.MaxRecommendedDensityOffset} for performance reasons"));

            return errors;

        }


    }
}
