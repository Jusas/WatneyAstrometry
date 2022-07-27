using System.Threading.Tasks;

namespace WatneyAstrometry.SolverVizTools.Abstractions;

public interface IDialogProvider
{
    Task<string[]> ShowOpenFileDialog(IWindow owner, string title, (string description, string[] extension)[] fileTypes,
        string initialDirectory, bool allowMultiple);
}