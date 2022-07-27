using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
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

    }
}
