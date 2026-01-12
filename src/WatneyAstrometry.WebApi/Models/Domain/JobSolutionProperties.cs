// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

// Domain models for job solution.

using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.WebApi.Models.Rest;

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Models.Domain
{
    public class JobSolutionProperties
    {
        public double Ra { get; set; }
        public double Dec { get; set; }
        public double FieldRadius { get; set; }
        public double Orientation { get; set; }
        public double PixScale { get; set; }
        public string Parity { get; set; }
        public double TimeSpent { get; set; }
        public int SearchIterations { get; set; }
        public int QuadMatches { get; set; }

        public JobSolutionFitsWcs FitsWcs { get; set; }

        /// <summary>
        /// Convert job model to REST model.
        /// </summary>
        public RestJobSolutionProperties ToRestJobSolutionProperties()
        {
            return new RestJobSolutionProperties()
            {
                Ra = Ra,
                Dec = Dec,
                FieldRadius = FieldRadius,
                Orientation = Orientation,
                PixScale = PixScale,
                SearchIterations = SearchIterations,
                QuadMatches = QuadMatches,
                FitsWcs = FitsWcs.ToRestJobSolutionFitsWcs(),
                Parity = Parity,
                TimeSpent = TimeSpent,
                Dec_dms = Conversions.DecDegreesToDdMmSs(Dec),
                Ra_hms = Conversions.RaDegreesToHhMmSs(Ra)
            };
        }
    }

    public class JobSolutionFitsWcs
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

        public RestJobSolutionFitsWcs ToRestJobSolutionFitsWcs()
        {
            return new RestJobSolutionFitsWcs()
            {
                Cd1_1 = Cd1_1,
                Cd1_2 = Cd1_2,
                Cd2_1 = Cd2_1,
                Cd2_2 = Cd2_2,
                Cdelt1 = Cdelt1,
                Cdelt2 = Cdelt2,
                Crota1 = Crota1,
                Crota2 = Crota2,
                Crpix1 = Crpix1,
                Crpix2 = Crpix2,
                Crval1 = Crval1,
                Crval2 = Crval2
            };
        }
    }
}
