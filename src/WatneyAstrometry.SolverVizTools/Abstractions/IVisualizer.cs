using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.SolverVizTools.Abstractions;

[Flags]
public enum VisualizationModes
{
    None = 0,
    Crosshair = 1 << 0,
    Grid = 1 << 1,
    DetectedStars = 1 << 2,
    Quads = 1 << 3,
    DeepSkyObjects = 1 << 4
}

public interface IVisualizer
{
    Task<Avalonia.Media.IImage> BuildVisualization(IImage sourceImage, SolveResult solveResult, 
        VisualizationModes flags);
}