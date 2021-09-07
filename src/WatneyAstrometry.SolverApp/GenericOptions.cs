using CommandLine;

namespace WatneyAstrometry.SolverApp
{
    public abstract class GenericOptions
    {
        [Option("use-config", Required = false, HelpText = "Path to configuration file. By default tries to load watney-solve-config.yml in the same directory where the solver is.")]
        public string ConfigPath { get; set; }

        [Option('i', "image", Required = true,
            HelpText = "The image file to solve.")]
        public string ImageFilename { get; set; }

        [Option("max-stars", Required = false, Default = 0, HelpText = "Maximum number of stars to use from the image. When not given, the solver decides itself. " +
            "When given, the solver uses this number. In cases of very high star count present in the image (wide-field images), the solve may fail if this number is not set high enough. " +
            "A low number of stars will speed up the solve, since it means less calculations are required, but there's a bigger chance that the solve will fail. 300 is generally a good value." +
            "A high number (> 1000) will however also affect performance due to the high number of calculations, and this gets especially noticeable with blind solves.")]
        public int MaxStars { get; set; }

        [Option("sampling", Required = false, Default = 0, HelpText = "Try to solve the field using a sampled set of database quads first. With sampling, we try to match " +
            "the image's star quads to only a fraction of the available database quads at a time, effectively making the search faster. The idea is that even if we can't find " +
            "a solution (enough matching quads), we still get potential matching areas with one or more matching quad, which we can then scan with a full set of database quads " +
            "to get the answer faster. Less work is performed in scanning, which makes it faster. Recommended (and default) value to use is 4 but some images may well solve " +
            "faster with higher values. Too high values will however result in time wasted in scanning and making the solve actually slower.")]
        public int Sampling { get; set; }

        [Option("out-format", Required = false, HelpText = "Output format. Valid values are 'json', 'tsv'.", Default = "json")]
        public string OutFormat { get; set; }

        [Option('o', "out", Required = false,
            HelpText = "Output file. If not set, output will be printed to stdout.")]
        public string OutFile { get; set; }

        [Option('x', "lower-density-offset", Required = false, Default = (uint)1,
            HelpText = "Include this many lower quad density passes in search (compared to image quad density).")]
        public uint LowerDensityOffset { get; set; }

        [Option('z', "higher-density-offset", Required = false, Default = (uint)1,
            HelpText = "Include this many higher quad density passes in search (compared to image quad density).")]
        public uint HigherDensityOffset { get; set; }

        [Option("extended", Required = false, Default = false,
            HelpText = "Produce extended output. This will print out a lot of additional detail about the solve.")]
        public bool ExtendedOutput { get; set; }
        
        [Option("log-stdout", Required = false, Default = false,
            HelpText = "Verbose logging. If true, will log a lot of additional lines to stdout.")]
        public bool LogToStdout { get; set; }

        [Option("log-file", Required = false, Default = "",
            HelpText = "Verbose logging. Give a filename to print verbose log lines into a file.")]
        public string LogToFile { get; set; }



    }
}