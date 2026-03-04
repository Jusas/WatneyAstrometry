namespace ConsoleSolverBenchmarkTool.UI;

public static class ConsolePrint
{
    public static string BigSeparator => string.Join("", Enumerable.Range(0, 70).Select(_ => "="));
    public static string SmallSeparator => string.Join("", Enumerable.Range(0, 70).Select(_ => "-"));
    
    public static void PrintBigSeparator() => Console.WriteLine(BigSeparator);
    public static void PrintSmallSeparator() => Console.WriteLine(SmallSeparator);
}