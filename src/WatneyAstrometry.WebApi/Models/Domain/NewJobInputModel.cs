// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models.Domain
{
    public class NewJobInputModel
    {
        public JobParametersModel Parameters { get; set; }
        
        internal IFormFile Image { get; set; }
        
    }
}
