// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Threading;
using CommandLine;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator
{
    public class Program
    {

        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public class Options
        {
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

            [Option('c', "cell", Required = false, HelpText = "Only generate a single cell, e.g. 'b00c00'. This is for debug purposes only.", Default = "")]
            public string SelectedCell { get; set; } = "";

            [Option("no-resume", Required = false, Default = false, HelpText = "By default we resume from position where processing was canceled if the flag is set. This flag allows " +
                "for canceling the job mid-way if needed, and then to continue later from that spot. If set to false, it forces the rebuilding of the entire database. The status is tracked " +
                "using a JSON file written to the output directory.")]
            public bool NoResume { get; set; }
        }

        static void Run(Options options)
        {
            Console.WriteLine($"Output directory is '{options.OutputDir}'");
            if (!Directory.Exists(options.OutputDir))
            {
                Console.WriteLine($"Creating directory {options.OutputDir}");
                Directory.CreateDirectory(options.OutputDir);
            }
            
            Console.WriteLine($"Building quads from stars with starting density of {options.StarsPerSqDeg} stars per sq degree");
            
            var builder = new DbBuilder(options);
            builder.Build(options.Threads, _cancellationTokenSource.Token);

        }

        static void Main(string[] args)
        {

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                Console.WriteLine("CTRL-C signaled! Stopping work...");
                _cancellationTokenSource.Cancel();
            };

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(Run);


        }
    }
}
