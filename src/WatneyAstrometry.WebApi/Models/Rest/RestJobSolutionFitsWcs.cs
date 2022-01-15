using AutoMapper;
using WatneyAstrometry.WebApi.Models.Domain;

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


    public class Mappings : AutoMapper.Profile
    {
        public Mappings()
        {
            // Explicit mappings.
            CreateMap<JobSolutionFitsWcs, RestJobSolutionFitsWcs>()
                .ValidateMemberList(MemberList.Destination)
                .ForMember(dest => dest.Crval1, x => x.MapFrom(src => src.Crval1))
                .ForMember(dest => dest.Crval2 , x => x.MapFrom(src => src.Crval2))
                .ForMember(dest => dest.Cd1_1, x => x.MapFrom(src => src.Cd1_1))
                .ForMember(dest => dest.Cd1_2, x => x.MapFrom(src => src.Cd1_2))
                .ForMember(dest => dest.Cd2_1, x => x.MapFrom(src => src.Cd2_1))
                .ForMember(dest => dest.Cd2_2, x => x.MapFrom(src => src.Cd2_2))
                .ForMember(dest => dest.Cdelt1, x => x.MapFrom(src => src.Cdelt1))
                .ForMember(dest => dest.Cdelt2, x => x.MapFrom(src => src.Cdelt2))
                .ForMember(dest => dest.Crota1, x => x.MapFrom(src => src.Crota1))
                .ForMember(dest => dest.Crota2, x => x.MapFrom(src => src.Crota2))
                .ForMember(dest => dest.Crpix1, x => x.MapFrom(src => src.Crpix1))
                .ForMember(dest => dest.Crpix2, x => x.MapFrom(src => src.Crpix2))
                ;
            

        }
    }
}