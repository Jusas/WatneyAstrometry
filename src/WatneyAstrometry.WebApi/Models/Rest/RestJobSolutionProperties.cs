#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest;

/// <summary>
/// Job solution data.
/// </summary>
public class RestJobSolutionProperties
{
    /// <summary>
    /// Image center RA coordinate.
    /// </summary>
    public double Ra { get; set; }

    /// <summary>
    /// Image center Dec coordinate.
    /// </summary>
    public double Dec { get; set; }
    
    /// <summary>
    /// Image center RA coordinate in HMS.
    /// </summary>
    public string Ra_hms { get; set; }
    
    /// <summary>
    /// Image center Dec coordinate in DMS.
    /// </summary>
    public string Dec_dms { get; set; }

    /// <summary>
    /// Image field radius in degrees.
    /// </summary>
    public double FieldRadius { get; set; }

    /// <summary>
    /// Image orientation.
    /// </summary>
    public double Orientation { get; set; }

    /// <summary>
    /// Pixel scale in arcsec per degree.
    /// </summary>
    public double PixScale { get; set; }

    /// <summary>
    /// Parity, either 'Normal' or 'Flipped'.
    /// </summary>
    public string Parity { get; set; }

    /// <summary>
    /// Time spent by the solver (seconds).
    /// </summary>
    public double TimeSpent { get; set; }

    /// <summary>
    /// Search iterations (areas searched) by the solver.
    /// </summary>
    public int SearchIterations { get; set; }

    /// <summary>
    /// The number of star quads that produced the solution.
    /// </summary>
    public int QuadMatches { get; set; }

    /// <summary>
    /// The FITS WCS records for the solution.
    /// </summary>
    public RestJobSolutionFitsWcs FitsWcs { get; set; }
    
}