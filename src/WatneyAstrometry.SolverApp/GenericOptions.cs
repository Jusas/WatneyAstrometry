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
            "A low number of stars can speed up the solve, since it means less calculations are required. 300 is generally a good value." +
            "A high number (> 1000) will however also affect performance due to the high number of calculations, and this gets especially noticeable with blind solves.")]
        public int MaxStars { get; set; }

        [Option("sampling", Required = false, Default = 0, HelpText = "Use a preliminary search run with sampled quads. With sampling, we will not get all the potential quads from the " +
            "database, we only get 1/<sampling> quads for which we run the first search round. This means less quads to compare to, which means less time spent in comparisons. If no solution is " +
            "found with sampling, then all best candidate areas with any quads found will get searched without sampling. The search will continue in sets of 1/<sampling> until all database quads " +
            "have been included. In most scenarios, this can significantly shorten the blind solve times to a fraction of what it would be without sampling. However at higher sampling values the overhead " +
            "from intermediary calculations can become so big that it starts to eat up the gains. Recommended to keep this at 32 or below. If not set, defaults to 24. If you do not wish to use sampling, use the value of 1.")]
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