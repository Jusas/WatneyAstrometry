using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models.Images;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class SolveProcessViewModel : ViewModelBase
    {
        private readonly IViewProvider _viewProvider;
        private readonly IDialogProvider _dialogProvider;
        private readonly IImageManager _imageManager;

        private IImage _solverImage;

        private IImage PlaceholderImage { get; set; }

        private ImageData _solverImageData;

        public ImageData[] SolverImageData
        {
            get => _solverImageData == null ? new ImageData[0] : new ImageData[] { _solverImageData };
        }

        public IImage SolverImage
        {
            get => _solverImage;
            set => this.RaiseAndSetIfChanged(ref _solverImage, value);
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
            Initialize();
        }

        public SolveProcessViewModel(IViewProvider viewProvider, IDialogProvider dialogProvider,
            IImageManager imageManager)
        {
            _viewProvider = viewProvider;
            _dialogProvider = dialogProvider;
            _imageManager = imageManager;
            Initialize();
        }
        
        private void Initialize()
        {
            PlaceholderImage = new Bitmap("z:\\firefox_61OAoUIBsW2.jpg");
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

    }
}
