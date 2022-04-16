// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Text.RegularExpressions;
using WatneyAstrometry.SolverApp.Exceptions;
using YamlDotNet.Serialization;

namespace WatneyAstrometry.SolverApp
{
    public class Configuration
    {
        [YamlMember(Alias = "quadDbPath")]
        public string QuadDatabasePath { get; set; }

        [YamlMember(Alias = "defaultMaxStars")]
        public uint? DefaultMaxStars { get; set; }

        [YamlMember(Alias = "defaultLowerDensityOffset")]
        public uint? DefaultLowerDensityOffset { get; set; }

        [YamlMember(Alias = "defaultHigherDensityOffset")]
        public uint? DefaultHigherDensityOffset { get; set; }

        [YamlMember(Alias = "defaultNearbySampling")]
        public uint? DefaultNearbySampling { get; set; }

        [YamlMember(Alias = "defaultNearbyParallelism")]
        public bool? DefaultNearbyParallelism { get; set; }

        [YamlMember(Alias = "defaultNearbySearchRadius")]
        public double? DefaultNearbySearchRadius { get; set; }
        
        [YamlMember(Alias = "defaultBlindMinRadius")]
        public double? DefaultBlindMinRadius { get; set; }

        [YamlMember(Alias = "defaultBlindMaxRadius")]
        public double? DefaultBlindMaxRadius { get; set; }

        [YamlMember(Alias = "defaultBlindSampling")]
        public uint? DefaultBlindSampling { get; set; }

        [YamlMember(Alias = "defaultBlindParallelism")]
        public bool? DefaultBlindParallelism { get; set; }

        [YamlMember(Alias = "defaultStarDetectionBgOffset")]
        public double? DefaultStarDetectionBgOffset { get; set; }


        public Configuration()
        {
        }

        public static Configuration Load(string configFile)
        {
            if (!File.Exists(configFile))
                throw new ConfigException($"Configuration file does not exist at path {configFile}");
            
            try
            {
                var configuration = File.ReadAllText(configFile);

                var deserializer = new DeserializerBuilder()
                    .Build();
                var config = deserializer.Deserialize<Configuration>(configuration);

                var relativePathRegex = new Regex(@"^(\.+)(\\|\/)");
                if (relativePathRegex.IsMatch(config.QuadDatabasePath))
                {
                    // If our Quad DB path is a relative path, try two paths;
                    // 1. Relative to executable
                    // 2. Relative to config file (if the config file is not in the same directory as the executable)

                    var executableDir =
                        Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                    var quadDbPath = Path.Combine(executableDir, config.QuadDatabasePath);

                    if (Directory.Exists(quadDbPath))
                        config.QuadDatabasePath = quadDbPath;
                    else if(Path.GetDirectoryName(configFile) != executableDir)
                    {
                        quadDbPath = Path.Combine(Path.GetDirectoryName(configFile), config.QuadDatabasePath);
                        if (Directory.Exists(quadDbPath))
                            config.QuadDatabasePath = quadDbPath;
                    }
                    
                }
                
                return config;
            }
            catch (Exception e)
            {
                throw new ConfigException("Failed to read or parse configuration file: " + e.Message, e);
            }
            
        }
    }
}