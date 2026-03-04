namespace ConsoleSolverBenchmarkTool.Services;

using ConsoleSolverBenchmarkTool.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class ConfigLoader
{
    public BenchmarkConfig Load(string path)
    {
        try
        {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<BenchmarkConfig>(yaml);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading config from '{path}': {ex.Message}");
            throw new Exception($"Failed to load config from '{path}'", ex);
        }
    }
}
