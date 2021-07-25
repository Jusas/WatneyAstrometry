using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace WatneyAstrometry.SolverApp
{
    [Verb("nearby", HelpText = "Perform nearby solve.")]
    public class NearbyOptions : GenericOptions
    {
        [Option('m', "manual", Required = false, SetName = "manual-params",
            HelpText = "Specifies that the center is provided using --ra and --dec parameters and scope radius in --field-radius.")]
        public bool UseManualParams { get; set; }

        [Option('r', "ra", Required = false, SetName = "manual-params",
            HelpText = "The search center in RA coordinate (either decimal or hours minutes seconds).")]
        public string Ra { get; set; }

        [Option('d', "dec", Required = false, SetName = "manual-params",
            HelpText = "The search center in Dec coordinate (either decimal or degrees minutes seconds).")]
        public string Dec { get; set; }

        [Option('f', "field-radius", SetName = "manual-params", Required = false,
            HelpText = "The (telescope) field radius (in degrees) to use.")]
        public float FieldRadius { get; set; }

        [Option('h', "use-fits-headers", Required = false, SetName = "auto-params",
            HelpText = "Specifies that the assumed center and field radius is provided by the file FITS headers. Will result in an error if the input file is not FITS or it does not contain the required FITS headers.")]
        public bool UseFitsHeaders { get; set; }

        [Option('s', "search-radius", Required = true,
            HelpText = "The search radius (deg), the solver search will cover this area around the center coordinate.")]
        public float SearchRadius { get; set; }

        [Option('p', "use-parallelism", Required = false, Default = false,
            HelpText = "Use parallelism, search multiple areas simultaneously.")]
        public bool UseParallelism { get; set; }

        [Usage(ApplicationAlias = "watney-solve")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Using FITS header preliminary coordinates", new NearbyOptions
                {
                    ImageFilename = "andromeda.fits",
                    SearchRadius = 10
                });
                yield return new Example("Manually defined coordinates, radius and density offsets", new NearbyOptions
                {
                    ImageFilename = "andromeda.png",
                    Ra = "10.7",
                    Dec = "41",
                    HigherDensityOffset = 1,
                    LowerDensityOffset = 1,
                    FieldRadius = 2,
                    SearchRadius = 10,
                    UseManualParams = true
                });
                yield return new Example("Manually defined coordinates in long form, radius and density offsets", new NearbyOptions
                {
                    ImageFilename = "andromeda.png",
                    Ra = "00 41 02",
                    Dec = "41 07 50",
                    HigherDensityOffset = 1,
                    LowerDensityOffset = 1,
                    FieldRadius = 2,
                    SearchRadius = 10,
                    UseManualParams = true
                });
            }
        }

    }
}