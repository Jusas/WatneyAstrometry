using ConsoleSolverBenchmarkTool.Config;
using ConsoleSolverBenchmarkTool.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConsoleSolverBenchmarkTool.Services;

public static class SolverConfigFileGenerator
{
    public static string GenerateTempConfigForRun(BenchmarkRunWithStatus run)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"watney-solve-config-{RandomString(8)}.yml");
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        
        // These are all we need; others are just overridable defaults.
        var yaml = serializer.Serialize(new
        {
            quadDbPath = run.Profile.Database.Config.Directory
        });
        File.WriteAllText(tempFilePath, yaml);
        return tempFilePath;
    }
    
    private static string RandomString(int length)
    {
        var random = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var str = Enumerable.Range(0, length)
            .Select(x => chars[random.Next(0, chars.Length)]);
        return new string(str.ToArray());
    }
}