using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.WebApi.Models;

public class ExtractedStarImage : IImageDimensions
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
}