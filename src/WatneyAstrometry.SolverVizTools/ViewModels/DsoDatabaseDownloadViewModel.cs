// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class DsoDatabaseDownloadViewModel : ViewModelBase
    {
        private readonly IDsoDatabase _dsoDatabase;

        public bool IsDownloading { get; set; }
        public bool IsPrompting => !IsDownloading;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private double _downloadProgress = 0;
        public double DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
        }

        public DsoDatabaseDownloadViewModel()
        {
        }

        public DsoDatabaseDownloadViewModel(IServiceProvider serviceProvider)
        {
            _dsoDatabase = serviceProvider.GetService<IDsoDatabase>();
        }

        public async Task DownloadDatabase()
        {
            IsDownloading = true;
            this.RaisePropertyChanged(nameof(IsDownloading));
            this.RaisePropertyChanged(nameof(IsPrompting));
            await Task.Yield();

            try
            {
                var filename = await _dsoDatabase.DownloadDatabase(ProgressCallback, _cancellationTokenSource.Token);
                OwnerWindow.Close(filename);
            }
            catch (Exception e)
            {
                MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Download error", e.Message,
                    ButtonEnum.Ok, Icon.Error);
                OwnerWindow.Close(null);
            }
            
        }

        private void ProgressCallback(double progress)
        {
            Dispatcher.UIThread.InvokeAsync(() => DownloadProgress = progress);
        }

        public void CloseWindow()
        {
            OwnerWindow.Close();
        }

    }
}
