﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.SolverVizTools.Models.Images;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface IImageManager
    {
        ImageData LoadImage(string filename);
    }
}