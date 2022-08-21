// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.SolverVizTools.Models.Images;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface IImageManager
    {
        Task<ImageData> LoadImage(string filename);
    }
}
