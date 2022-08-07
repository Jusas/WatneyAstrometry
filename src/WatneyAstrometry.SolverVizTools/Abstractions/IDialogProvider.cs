using System.Threading.Tasks;

namespace WatneyAstrometry.SolverVizTools.Abstractions;


public enum DialogIcon
{
    None,
    Info,
    Error
}

public interface IDialogProvider
{
    Task<string[]> ShowOpenFileDialog(IWindow owner, string title, (string description, string[] extension)[] fileTypes,
        string initialDirectory, bool allowMultiple);
    Task<string> ShowOpenFolderDialog(IWindow owner, string title, string initialDirectory);

    Task<string> ShowSaveFileDialog(IWindow owner, string title, string initialDirectory, string initialFilename,
        string defaultExtension);

    Task ShowMessageBox(IWindow owner, string title, string message, DialogIcon icon = DialogIcon.None);


    Task<bool> ShowMessageBoxYesNo(IWindow owner, string title, string message, int? minHeight = null,
        int? minWidth = null);
}