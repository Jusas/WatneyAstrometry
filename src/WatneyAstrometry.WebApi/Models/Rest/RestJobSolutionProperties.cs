using AutoMapper;
using WatneyAstrometry.WebApi.Models.Domain;

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


    public class Mappings : AutoMapper.Profile
    {
        public Mappings()
        {
            // Explicit mappings.
            CreateMap<JobSolutionProperties, RestJobSolutionProperties>()
                .ValidateMemberList(MemberList.Destination)
                .ForMember(dest => dest.Ra, x => x.MapFrom(src => src.Ra))
                .ForMember(dest => dest.Dec, x => x.MapFrom(src => src.Dec))
                .ForMember(dest => dest.FieldRadius, x => x.MapFrom(src => src.FieldRadius))
                .ForMember(dest => dest.Orientation, x => x.MapFrom(src => src.Orientation))
                .ForMember(dest => dest.PixScale, x => x.MapFrom(src => src.PixScale))
                .ForMember(dest => dest.Parity, x => x.MapFrom(src => src.Parity))
                .ForMember(dest => dest.TimeSpent, x => x.MapFrom(src => src.TimeSpent))
                .ForMember(dest => dest.SearchIterations, x => x.MapFrom(src => src.SearchIterations))
                .ForMember(dest => dest.QuadMatches, x => x.MapFrom(src => src.QuadMatches))
                .ForMember(dest => dest.FitsWcs, x => x.MapFrom(src => src.FitsWcs))
                ;


        }
    }
}