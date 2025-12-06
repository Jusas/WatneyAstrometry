// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

// Example, working dir in bin/Debug/net9.0/
//   ./watney-bench ../../../config.json ../../../
//
// Read the JSON config, run different combinations, save each solve result to a file, and finally
// create CSV files from the results, one CSV per image. We are interested in each image, how it solves
// with different configurations.

using System.Text.Json;
using Benchmark;

if (args.Length < 1)
{
    Console.WriteLine("Usage: ./watney-bench <config json file path> <datafiles directory root>");
    Console.WriteLine("Data files directory root is the directory under which benchmark image, quad db and watney binary directories are located.");
    return 1;
}

if (args.Length < 2)
{
    Console.WriteLine("Need the config file path as the first argument");
    Console.WriteLine("Need the data directories root as the second argument");
    return 1;
}

if (!File.Exists(args[0]))
{
    Console.WriteLine($"{args[0]}: File not found");
    return 1;
}

BenchmarkConfig.DataRootDirectory = args[1];

using var stream = File.OpenRead(args[0]);
try
{
    var config = BenchmarkConfig.FromJson(stream);
    var runner = new Runner(config);
    runner.PrintSummary();
    runner.RunBlindBenchmarking();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}





return 0;
