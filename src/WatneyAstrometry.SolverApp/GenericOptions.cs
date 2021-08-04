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
            "A high number (> 1000) will however also affect performance due to the high number of calculations, especially noticeable with blind solves.")]
        public int MaxStars { get; set; }

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