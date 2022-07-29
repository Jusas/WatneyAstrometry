using ReactiveUI;

namespace WatneyAstrometry.SolverVizTools.Models;

public class WatneyConfiguration : ReactiveObject
{
    private string _quadDatabasePath;

    public string QuadDatabasePath
    {
        get => _quadDatabasePath;
        set => this.RaiseAndSetIfChanged(ref _quadDatabasePath, value);
    }
}