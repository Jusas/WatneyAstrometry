using System;
using System.IO;
using YamlDotNet.Serialization;

namespace WatneyAstrometry.SolverApp
{
    public class Configuration
    {
        [YamlMember(Alias = "quadDbPath")]
        public string QuadDatabasePath { get; set; }

        public Configuration()
        {
        }

        public static Configuration Load(string configFile)
        {
            if (!File.Exists(configFile))
                throw new Exception($"Configuration file does not exist at path {configFile}");

            var configuration = File.ReadAllText(configFile);

            try
            {
                var deserializer = new DeserializerBuilder()
                    .Build();
                return deserializer.Deserialize<Configuration>(configuration);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse configuration file. " + e.Message);
            }
            
        }
    }
}