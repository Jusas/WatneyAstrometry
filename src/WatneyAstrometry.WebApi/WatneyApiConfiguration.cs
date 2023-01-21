// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using YamlDotNet.Serialization;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi
{
    public class WatneyApiConfiguration
    {
        [YamlMember(Alias = "quadDbPath")]
        public string QuadDatabasePath { get; set; }

        [YamlMember(Alias = "usePersistency", SerializeAs = typeof(bool))]
        public bool UsePersistency { get; set; }

        [YamlMember(Alias = "workDirectory")]
        public string WorkDirectory { get; set; }

        [YamlMember(Alias = "maxImageSizeBytes", SerializeAs = typeof(long))]
        public long MaxImageSizeBytes { get; set; }

        [YamlMember(Alias = "jobLifetime", SerializeAs = typeof(TimeSpan))]
        public TimeSpan JobLifetime { get; set; }

        [YamlMember(Alias = "solverTimeoutValue", SerializeAs = typeof(TimeSpan))]
        public TimeSpan SolverTimeoutValue { get; set; }

        [YamlMember(Alias = "allowedConcurrentSolves", SerializeAs = typeof(int))]
        public int AllowedConcurrentSolves { get; set; }

        [YamlMember(Alias = "enableSwagger", SerializeAs = typeof(bool))]
        public bool EnableSwagger { get; set; }

        [YamlMember(Alias = "authentication")]
        public string Authentication { get; set; }
        
        [YamlMember(Alias = "apikeys")]
        public string ApiKeyFile { get; set; }
        
        [YamlMember(Alias = "enableAstrometryNetCompatibilityApi", SerializeAs = typeof(bool))]
        public bool EnableCompatibilityApi { get; set; }
        
        [YamlMember(Alias = "starDetectionBgOffset", SerializeAs = typeof(double))]
        public double StarDetectionBgOffset { get; set; }

        [YamlMember(Alias = "limitThreads")]
        public int? LimitThreads { get; set; }

        [YamlIgnore]
        public IReadOnlyDictionary<string, string> ApiKeys { get; set; }

        private string _executableDirectory;

        public WatneyApiConfiguration()
        {
        }

        public static WatneyApiConfiguration Load(string configFile)
        {
            if (!File.Exists(configFile))
                throw new Exception($"Configuration file does not exist at path {configFile}");
            
            var configuration = File.ReadAllText(configFile);

            try
            {
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                var config = deserializer.Deserialize<WatneyApiConfiguration>(configuration);
                config._executableDirectory = // Hmm, do I need this?
                    Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

                if (!string.IsNullOrEmpty(config.ApiKeyFile) && "apikey".Equals(config.Authentication, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!File.Exists(config.ApiKeyFile))
                        throw new Exception("apikeys file does not exist");

                    var apiKeyContent = File.ReadAllText(config.ApiKeyFile);
                    config.ApiKeys = deserializer.Deserialize<Dictionary<string, string>>(apiKeyContent);
                }

                return config;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse configuration file. " + e.Message);
            }
            
        }
    }
}