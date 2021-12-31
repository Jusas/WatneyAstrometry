using Microsoft.AspNetCore.Mvc;

namespace WatneyAstrometry.WebApi.Models
{
    public class JobFormUnifiedModel
    {
        //[ModelBinder(BinderType = typeof(SubmissionDataJsonBinder))]
        [FromForm(Name = "args")]
        public JobParametersModel Parameters { get; set; }
        
        internal IFormFile Image { get; set; }

        public string[] Validate()
        {
            // TODO make global constants of the different limits ...

            var errors = new List<string>();

            if(Image == null)
                errors.Add("Image was not defined.");

            if(Image?.Length == 0)
               errors.Add("Image size is zero bytes.");

            var fileExtension = Path.GetExtension(Image.FileName).ToLowerInvariant();
            var supportedImageExtensions = new[] { ".fit", ".fits", ".png", ".jpg", ".jpeg" };
            if(supportedImageExtensions.All(e => e != fileExtension))
                errors.Add($"Unsupported file extension. Supported file extensions are: {string.Join(", ", supportedImageExtensions)}.");


            if (Parameters.Mode == JobParametersModel.SolveMode.Nearby)
            {
                var useFitsHeaders = Parameters.NearbyParameters?.UseFitsHeaders ?? false;
                var p = Parameters.NearbyParameters;

                if (p.SearchRadius == null)
                    errors.Add("searchRadius must be set for nearby solves");

                if (!useFitsHeaders && (p?.Ra == null || p.Dec == null || p.FieldRadius == null))
                    errors.Add($"ra, dec and fieldRadius must be provided when useFitsHeaders is false");

            }

            if (Parameters.Mode == JobParametersModel.SolveMode.Blind)
            {
                var p = Parameters.BlindParameters;
                if (p?.MaxRadius == null || p.MinRadius == null)
                    errors.Add($"maxRadius and minRadius must be set when ");

            }

            return errors.ToArray();

        }
    }
}
