#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest;

/// <summary>
/// WCS records to embed in FITS.
/// </summary>
public class RestJobSolutionFitsWcs
{
    public double Cd1_1 { get; set; }
    public double Cd1_2 { get; set; }
    public double Cd2_1 { get; set; }
    public double Cd2_2 { get; set; }
    public double Cdelt1 { get; set; }
    public double Cdelt2 { get; set; }
    public double Crota1 { get; set; }
    public double Crota2 { get; set; }
    public double Crpix1 { get; set; }
    public double Crpix2 { get; set; }
    public double Crval1 { get; set; }
    public double Crval2 { get; set; }
    
}