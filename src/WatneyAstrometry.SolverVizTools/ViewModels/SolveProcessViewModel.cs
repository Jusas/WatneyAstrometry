using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.DI;
using WatneyAstrometry.SolverVizTools.Models.Images;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class SolveProcessViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewProvider _viewProvider;
        private readonly IDialogProvider _dialogProvider;
        private readonly IImageManager _imageManager;

        private IImage _solverImage;

        private IImage PlaceholderImage { get; set; }

        private ImageData _solverImageData;
        private readonly IAssetLoader _assetManager;

        public ImageData[] SolverImageData
        {
            get => _solverImageData == null ? new ImageData[0] : new ImageData[] { _solverImageData };
        }

        public IImage SolverImage
        {
            get => _solverImage;
            set => this.RaiseAndSetIfChanged(ref _solverImage, value);
        }

        private bool _isSolving = false;

        public bool IsSolving
        {
            get => _isSolving;
            set => this.RaiseAndSetIfChanged(ref _isSolving, value);
        }

        public bool ToolbarButtonsEnabled
        {
            get
            {
                return !_isSolving && SolverImage != PlaceholderImage;
            }
        }

        public bool OpenImageButtonEnabled
        {
            get
            {
                return !IsSolving;
            }
        }

        public bool SolveButtonEnabled
        {
            get
            {
                return !_isSolving && SolverImage != PlaceholderImage;
            }
        }

        public bool CancelSolveButtonEnabled
        {
            get
            {
                return _isSolving;
            }
        }

        public bool PlaceHolderTextsVisible
        {
            get
            {
                return PlaceholderImage == SolverImage;
            }
        }

        private string _solverElapsedSeconds = "0.0";

        public string SolverElapsedSeconds
        {
            get => _solverElapsedSeconds;
            set => this.RaiseAndSetIfChanged(ref _solverElapsedSeconds, value);
        }

        public SolveProcessViewModel()
        {
            // Mock data
            _solverImageData = new ImageData
            {
                FileName = "test.png",
                SourceFormat = "PNG",
                Width = 1024,
                Height = 768
            };
            _serviceProvider = new ServiceProvider();
            _assetManager = _serviceProvider.GetAvaloniaService<IAssetLoader>();
            Initialize();
        }

        public SolveProcessViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewProvider = serviceProvider.GetService<IViewProvider>();
            _dialogProvider = serviceProvider.GetService<IDialogProvider>();
            _imageManager = serviceProvider.GetService<IImageManager>();
            _assetManager = serviceProvider.GetAvaloniaService<IAssetLoader>();
            Initialize();
        }
        
        private void Initialize()
        {
            PlaceholderImage = new Bitmap(_assetManager.Open(new Uri("avares://WatneyAstrometry.SolverVizTools/Assets/placeholder.jpg")));
            //PlaceholderImage = new Bitmap("z:\\firefox_61OAoUIBsW2.jpg");
            SolverImage = PlaceholderImage;
        }

        public async Task OpenImageViaDialog()
        {
            
            var fileNames = await _dialogProvider.ShowOpenFileDialog(OwnerWindow, "Select image file",
                new (string description, string[] extension)[]
                {
                    ("FITS files", new[] { "fits", "fit" }),
                    ("Common image formats", new[] { "jpg", "jpeg", "png" })
                }, "", false);

            var filename = fileNames?.FirstOrDefault();

            if (filename != null)
            {
                var imageData = _imageManager.LoadImage(filename);
                SolverImage = imageData.UiImage;
                _solverImageData = imageData;
                this.RaisePropertyChanged(nameof(SolverImageData));
            }

        }

        public async Task StartSolve()
        {

        }

    }
}
