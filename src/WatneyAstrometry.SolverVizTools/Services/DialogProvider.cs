// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MessageBox.Avalonia.DTO;
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

        public async Task ShowMessageBox(IWindow owner, string title, string message, DialogIcon icon = DialogIcon.None)
        {
            var boxIcon = icon switch
            {
                DialogIcon.None => Icon.None,
                DialogIcon.Info => Icon.Info,
                DialogIcon.Error => Icon.Error
            };

            var box = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(title, message, ButtonEnum.Ok, boxIcon);
            await box.ShowDialog((owner.NativeWindow as Window)!);
        }

        public async Task<bool> ShowMessageBoxYesNo(IWindow owner, string title, string message, int? minHeight = null, int? minWidth = null)
        {
            var @params = new MessageBoxStandardParams();
            @params.ButtonDefinitions = ButtonEnum.YesNo;
            @params.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            @params.ContentTitle = title;
            @params.ContentHeader = message;
            if (minHeight != null)
                @params.MinHeight = minHeight.Value;
            if (minWidth != null)
                @params.MinWidth = minWidth.Value;

            var box = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(@params);
            
            var result = await box.ShowDialog((owner.NativeWindow as Window)!);
            return (result & ButtonResult.Yes) != 0;
        }

    }
}
