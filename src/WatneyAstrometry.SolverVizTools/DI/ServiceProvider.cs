using Splat;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.DI;

/// <summary>
/// This abstracts away the worst of the Splat's locator antipattern.
/// </summary>
public class ServiceProvider : IServiceProvider
{
    public T GetService<T>()
    {
        return Locator.Current.GetService<T>();
    }
}