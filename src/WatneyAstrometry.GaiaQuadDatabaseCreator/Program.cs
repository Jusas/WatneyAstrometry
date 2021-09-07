using System;
using System.IO;
using CommandLine;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator
{
    public class Program
    {

        public class Options
        {
            //[Option('f', "fieldsize", Required = true, HelpText = "Field diameter in degrees for which sized images this data set gets optimized for", Default = 1.0)]
            //public double FieldSize { get; set; }

            // Base stars per degree. Each pass (Passes) creates a pass with PassFactor * stars  (stars, PassFactor*stars, PassFactor*PassFactor*stars, etc) of which we make quads.

            [Option('s', "stars", Required = true, HelpText = "Stars per square degree", Default = 20)] // Density (stars per deg) = stars / resolution
            public int StarsPerSqDeg { get; set; }
            
            [Option('o', "out", Required = true, HelpText = "Output directory")]
            public string OutputDir { get; set; }

            [Option('i', "input", Required = true, HelpText = "Input file directory, that contains a set of .stars files created using GaiaStarExtractor")]
            public string InputDir { get; set; }

            [Option('t', "threads", Required = true, HelpText = "Threads to use. Defaults to detected logical processor count count - 1 (or 1 if only one is detected)")]
            public int Threads { get; set; } = -1;
            
            [Option("start-pass", Required = false, HelpText = "Which pass to start on. Passes are the number of times to gather the quads, with each pass increasing the number of stars included", Default = 0)]
            public int StartPass { get; set; } = 0;

            [Option("end-pass", Required = false, HelpText = "Which pass the run ends in. Passes are the number of times to gather the quads, with each pass increasing the number of stars included", Default = 9)]
            public int EndPass { get; set; } = 9;

            [Option('x', "passfactor", Required = false, HelpText = "Base factor to use when increasing number of stars per pass. This number gets raised to the power [pass], i.e. passfactor^passNumber. Defaults to sqrt(2).", Default = 1.4142135623730950488016887242097f)]
            public float PassFactor { get; set; } = (float)Math.Sqrt(2);

            [Option('c', "cell", Required = false, HelpText = "Only generate a single cell, e.g. 'b00c00'", Default = "")]
            public string SelectedCell { get; set; } = "";
        }

        static void Run(Options options)
        {
            Console.WriteLine($"Output directory is '{options.OutputDir}'");
            if (!Directory.Exists(options.OutputDir))
            {
                Console.WriteLine($"Creating directory {options.OutputDir}");
                Directory.CreateDirectory(options.OutputDir);
            }
            // in reality, with parameters like -p 4 -x 0.85 we produce a nice range: 0.5 ==> 0.5..1,  1 ==> 1..2, 2 ==> 2..4, 4 ==> 4..8 and so on

            //var fieldSquareSideLen = options.FieldSize * Math.Sin(Math.PI / 4);
            //var starsPerSqDegree = (options.StarsPerSqDeg / (fieldSquareSideLen * fieldSquareSideLen));

            Console.WriteLine($"Building quads from stars with target {options.StarsPerSqDeg} stars per sq degree");

            //Console.WriteLine($"Building quads from stars with target field diameter of {options.FieldSize} degrees, approx density of {starsPerSqDegree:F1} stars per sq degree");

            var builder = new DbBuilder(options);
            builder.Build(options.Threads);

        }

        static void Main(string[] args)
        {

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);


        }
    }
}
