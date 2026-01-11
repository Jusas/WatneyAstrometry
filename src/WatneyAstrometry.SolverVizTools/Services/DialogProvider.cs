// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class DialogProvider : IDialogProvider
    {

        public async Task<string[]> ShowOpenFileDialog(IWindow owner, string title, (string description, string[] extension)[] fileTypes, 
            string initialDirectory, bool allowMultiple)
        {
            var storageProvider = TopLevel.GetTopLevel(owner.NativeWindow as Window)!.StorageProvider;
            var startBrowseLocation = !string.IsNullOrEmpty(initialDirectory)
                ? await storageProvider.TryGetFolderFromPathAsync(new Uri(initialDirectory))
                : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            
            var selection = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                AllowMultiple = allowMultiple,
                Title = title, 
                SuggestedStartLocation = startBrowseLocation,
                FileTypeFilter = fileTypes.Select(x => 
                    new FilePickerFileType(x.description) { Patterns = x.extension })
                    .ToArray()
            });

            return selection.Select(x => x.TryGetLocalPath()).ToArray();

        }

        public async Task<string> ShowOpenFolderDialog(IWindow owner, string title, string initialDirectory)
        {
            
            var storageProvider = TopLevel.GetTopLevel(owner.NativeWindow as Window)!.StorageProvider;
            var startBrowseLocation = !string.IsNullOrEmpty(initialDirectory)
                ? await storageProvider.TryGetFolderFromPathAsync(new Uri(initialDirectory))
                : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            var selectedDirectory = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false,
                Title = title, 
                SuggestedStartLocation = startBrowseLocation
            });

            if (selectedDirectory.Count > 0)
                return selectedDirectory[0].TryGetLocalPath();
            
            return null;
            
        }

        public async Task<string> ShowSaveFileDialog(IWindow owner, string title, string initialDirectory, string initialFilename, string defaultExtension)
        {
            
            var storageProvider = TopLevel.GetTopLevel(owner.NativeWindow as Window)!.StorageProvider;
            var startBrowseLocation = !string.IsNullOrEmpty(initialDirectory)
                ? await storageProvider.TryGetFolderFromPathAsync(new Uri(initialDirectory))
                : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            
            var filename = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = title, 
                SuggestedStartLocation = startBrowseLocation,
                DefaultExtension =  defaultExtension,
                ShowOverwritePrompt = true,
                SuggestedFileName = initialFilename
            });
            
            return filename?.TryGetLocalPath();

        }

        public async Task ShowMessageBox(IWindow owner, string title, string message, DialogIcon icon = DialogIcon.None)
        {
            var boxIcon = icon switch
            {
                DialogIcon.None => Icon.None,
                DialogIcon.Info => Icon.Info,
                DialogIcon.Error => Icon.Error
            };

            var box = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, boxIcon);
            await box.ShowAsPopupAsync((owner.NativeWindow as Window)!);
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

            var box = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(@params);
            
            var result = await box.ShowWindowDialogAsync((owner.NativeWindow as Window)!);
            return (result & ButtonResult.Yes) != 0;
        }

    }
}
