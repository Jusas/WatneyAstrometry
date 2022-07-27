namespace WatneyAstrometry.SolverVizTools.Abstractions;

public interface IServiceProvider
{
    T GetService<T>();
    T GetAvaloniaService<T>();
}