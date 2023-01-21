// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using CommandLine;

namespace WatneyAstrometry.SolverApp
{
    public abstract class GenericOptions
    {
        [Option("use-config", Required = false, HelpText = "Path to configuration file. By default tries to load watney-solve-config.yml in the same directory where the solver is.")]
        public string ConfigPath { get; set; }

        [Option('i', "image", Required = false,
            HelpText = "The image file to solve.", Group = "input")]
        public string ImageFilename { get; set; }

        [Option("image-stdin", Required = false, Default = false, HelpText = "Read image from stdin. If this is true, the --image parameter is ignored.", Group = "input")]
        public bool ImageFromStdin { get; set; }
        
        [Option("xyls", Required = false, HelpText = "X,Y list file (stars), a .xyls file to solve. " +
            "A .xyls file is a FITS file with a binary table, which can for example be created using SExtractor. " +
            "When used, you must also provide the --xyls-imagesize parameter.", Group = "input")]
        public string XylsFilename { get; set; }
        
        [Option("xyls-stdin", Required = false, Default = false, HelpText = "Read X,Y list file from stdin. " +
            "A .xyls file is a FITS file with a binary table, which can for example be created using SExtractor. " +
            "If this is true, the --image parameter is ignored.", Group = "input")]
        public bool XylsFromStdin { get; set; }
        
        [Option("xyls-imagesize", Required = false, HelpText = "The image size in pixels <X>x<Y>. Example: --xyls-imagesize 800x600")]
        public string XylsImageSize { get; set; }

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

        [Option('w', "wcs", Required = false,
            HelpText = "Output filename for WCS coordinates. If given, a FITS file containing WCS headers will be produced.")]
        public string WcsFile { get; set; }

        [Option('x', "lower-density-offset", Required = false,
            HelpText = "Include this many lower quad density passes in search (compared to image quad density). Default value is 1.")]
        public uint? LowerDensityOffset { get; set; }

        [Option('z', "higher-density-offset", Required = false,
            HelpText = "Include this many higher quad density passes in search (compared to image quad density). Default value is 1.")]
        public uint? HigherDensityOffset { get; set; }

        [Option("extended", Required = false, Default = false,
            HelpText = "Produce extended output. This will print out a lot of additional detail about the solve, including the FITS header keywords/values and the CD matrix.")]
        public bool ExtendedOutput { get; set; }
        
        [Option("log-stdout", Required = false, Default = false,
            HelpText = "Verbose logging. If true, will log a lot of additional lines to stdout.")]
        public bool LogToStdout { get; set; }

        [Option("log-file", Required = false, Default = "",
            HelpText = "Verbose logging. Give a filename to print verbose log lines into a file.")]
        public string LogToFile { get; set; }

        [Option("benchmark", Required = false, Default = false,
            HelpText = "Benchmark mode, prints some output to stdout.")]
        public bool Benchmark { get; set; }

        [Option("limit-threads", Required = false, Default = 0, 
            HelpText = "Limit the number of threads used by the solver (by default uses as many as there are CPU logical cores)")]
        public int LimitThreads { get; set; }
        



    }
}