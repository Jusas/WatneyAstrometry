// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.


using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WatneyAstrometry.SolverVizTools.Models.Images
{
    public class ImageData
    {
        /// <summary>
        /// Image shown in the UI image control.
        /// </summary>
        public Avalonia.Media.IImage UiImage { get; set; }

        /// <summary>
        /// Image that Watney needs for solving.
        /// </summary>
        public WatneyAstrometry.Core.Image.IImage WatneyImage { get; set; }
        
        /// <summary>
        /// Image that is used for manipulation, we use ImageSharp to draw things, then update the UI image.
        /// </summary>
        public Image<Rgba32> EditableImage { get; set; }

        public string FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string SourceFormat { get; set; }


    }
}
