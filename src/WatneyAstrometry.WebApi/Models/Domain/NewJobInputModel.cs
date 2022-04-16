// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models.Domain
{
    /// <summary>
    /// A new job model.
    /// </summary>
    public class NewJobInputModel
    {
        /// <summary>
        /// Input parameters for the solver.
        /// </summary>
        public JobParametersModel Parameters { get; set; }
        
        internal IFormFile Image { get; set; }
        
    }
}
