using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MessageBox.Avalonia.Enums;
using Microsoft.CodeAnalysis;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class DialogProvider : IDialogProvider
    {

        public async Task<string[]> ShowOpenFileDialog(IWindow owner, string title, (string description, string[] extension)[] fileTypes, 
            string initialDirectory, bool allowMultiple)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.AllowMultiple = allowMultiple;
            dialog.Filters = fileTypes.Select(x => new FileDialogFilter()
            {
                Name = x.description,
                Extensions = x.extension.ToList()
            }).ToList();
            dialog.Directory = initialDirectory;
            var selection = await dialog.ShowAsync((owner.NativeWindow as Window)!);
            return selection;
        }

        public async Task<string> ShowOpenFolderDialog(IWindow owner, string title, string initialDirectory)
        {
            var dialog = new OpenFolderDialog();
            dialog.Directory = initialDirectory;
            dialog.Title = title;
            var directory = await dialog.ShowAsync((owner.NativeWindow as Window)!);
            return directory;
        }

        public async Task<string> ShowSaveFileDialog(IWindow owner, string title, string initialDirectory, string initialFilename, string defaultExtension)
        {
            var dialog = new SaveFileDialog();
            dialog.DefaultExtension = defaultExtension;
            dialog.Directory = initialDirectory;
            dialog.InitialFileName = initialFilename;
            dialog.Title = title;
            var filename = await dialog.ShowAsync((owner.NativeWindow as Window)!);
            return filename;
        }

        public async Task ShowMessageBox(IWindow owner, string title, string message)
        {
            var box = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(title, message, ButtonEnum.Ok);
            await box.ShowDialog((owner.NativeWindow as Window)!);
        }

        
    }
}
