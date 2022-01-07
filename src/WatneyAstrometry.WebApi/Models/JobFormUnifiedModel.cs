// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WatneyAstrometry.WebApi.Models
{
    public class JobFormUnifiedModel
    {
        [FromForm(Name = "args")]
        public JobParametersModel Parameters { get; set; }
        
        internal IFormFile Image { get; set; }

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

                if (!useFitsHeaders)
                {
                    if((p?.Ra == null || p.Dec == null || p.FieldRadius == null))
                        errors.Add("useFitsHeaders", arr($"ra, dec and fieldRadius must be provided when useFitsHeaders is false"));
                }
            }

            if (Parameters.Mode == "blind")
            {
                var p = Parameters.BlindParameters;
                if (p?.MaxRadius == null)
                    errors.Add("maxRadius", arr("maxRadius must be set when solve mode is blind"));
                if (p?.MinRadius == null)
                    errors.Add("minRadius", arr("minRadius must be set when solve mode is blind"));

            }

            return errors;

        }


    }
}
