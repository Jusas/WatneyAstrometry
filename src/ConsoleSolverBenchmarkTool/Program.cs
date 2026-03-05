using ConsoleSolverBenchmarkTool.UI;

namespace ConsoleSolverBenchmarkTool;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var configPath = args.Length > 0 ? args[0] : "config.yaml";
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Config file not found: {configPath}");
                Console.Error.WriteLine("Usage: ConsoleSolverBenchmarkTool [config.yaml]");
                Environment.Exit(1);
            }

            var loop = new InteractionLoop(configPath);
            await loop.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
