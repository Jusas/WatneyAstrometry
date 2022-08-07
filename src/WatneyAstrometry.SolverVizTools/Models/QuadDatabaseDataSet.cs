using System.IO;
using System.Linq;
using ReactiveUI;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.SolverVizTools.Models;

public class QuadDatabaseDataSet : ReactiveObject
{
    private const string FilenamePrefix = "gaia2";

    private bool _isDownloaded = false;
    public bool IsDownloaded
    {
        get => _isDownloaded;
        set
        {
            this.RaiseAndSetIfChanged(ref _isDownloaded, value);
            this.RaisePropertyChanged(nameof(IsDownloadable));
        }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadProgress, value);
            this.RaisePropertyChanged(nameof(IsDownloading));
            this.RaisePropertyChanged(nameof(IsDownloadable));
        }
    }

    public bool IsDownloadable
    {
        get => !IsDownloading && !IsDownloaded;
    }

    public bool IsDownloading
    {
        get => _downloadProgress > 0 && _downloadProgress < 100;
    }
    
    public string Url { get; set; }
    public int StartPass { get; set; }
    public int EndPass { get; set; }
    public int BaseDensity { get; set; }
    private string[] CellFilenames =>
        SkySegmentSphere.Cells.Select(x => BuildCellFilename(x)).ToArray();

    private string IndexFilename =>
        $"{FilenamePrefix}-{StartPass:00}-{EndPass:00}-{BaseDensity}.qdbindex";

    private string BuildCellFilename(Cell cell) =>
        $"{FilenamePrefix}-{cell.CellId}-{StartPass:00}-{EndPass:00}-{BaseDensity}.qdb";

    public string Size { get; set; }
    public string Description { get; set; }
    public QuadDatabaseDataSet(string url, int startPass, int endPass, int baseDensity, string size, string description)
    {
        Url = url;
        StartPass = startPass;
        EndPass = endPass;
        BaseDensity = baseDensity;
        Size = size;
        Description = description;
    }

    public void CheckAndUpdateIsDownloaded(string directory)
    {
        var filename = Path.Combine(directory, IndexFilename);
        if (!File.Exists(filename))
        {
            IsDownloaded = false;
            return;
        }

        var cells = CellFilenames;
        foreach (var cellFilename in cells)
        {
            filename = Path.Combine(directory, cellFilename);
            if (!File.Exists(filename))
            {
                IsDownloaded = false;
                return;
            }
        }

        IsDownloaded = true;
    }
}