using YamlDotNet.Serialization;

namespace WatneyAstrometry.WebApi
{
    public class WatneyApiConfiguration
    {
        [YamlMember(Alias = "quadDbPath")]
        public string QuadDatabasePath { get; set; }

        [YamlMember(Alias = "usePersistency")]
        public string UsePersistency { get; set; }

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
        
        [YamlMember(Alias = "apikey")]
        public string ApiKey { get; set; }
        
        [YamlMember(Alias = "enableAstrometryNetCompatibilityApi", SerializeAs = typeof(bool))]
        public bool EnableCompatibilityApi { get; set; }

        

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
                return config;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to parse configuration file. " + e.Message);
            }
            
        }
    }
}