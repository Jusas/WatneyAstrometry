using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.DI;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Models.Images;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    
    public enum SolveUiState
    {
        Uninitialized = 0,
        ImageOpening = 1,
        ImageLoaded = 2,
        Solving = 3,
        SolveCompleteSuccess = 4,
        SolveCompleteFailure = 5
    }

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

        public ImageData SolverImageData
        {
            get => _solverImageData;
            set => this.RaiseAndSetIfChanged(ref _solverImageData, value);
        }


        public IImage SolverImage
        {
            get => _solverImage;
            set => this.RaiseAndSetIfChanged(ref _solverImage, value);
        }

        public bool IsSolving => SolveUiState == SolveUiState.Solving;
        public bool ToolbarButtonsEnabled => SolveUiState > SolveUiState.Solving && !IsBusyVisualizing;
        public bool OpenImageButtonEnabled => SolveUiState != SolveUiState.Solving;
        public bool SolveButtonEnabled => SolveUiState > SolveUiState.Uninitialized && SolveUiState != SolveUiState.Solving;

        public bool SaveResultsButtonsEnabled =>
            SolveUiState == SolveUiState.SolveCompleteSuccess && _solveResult != null;
        public bool CancelSolveButtonEnabled => SolveUiState == SolveUiState.Solving;
        public bool PlaceHolderTextsVisible => SolveUiState == SolveUiState.Uninitialized;
        public string ImageInfoLabel { get; set; }

        private string _solverElapsedSeconds = "0.0";

        private Solver _solverInstance;
        private CompactQuadDatabase _quadDatabaseInstance;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _counterTask;
        private Stopwatch _starDetectionStopwatch;
        private Stopwatch _solverStopwatch;
        private Stopwatch _fullSolveProcessStopwatch;

        private SolveResult _solveResult;

        public string SolverElapsedSeconds
        {
            get => _solverElapsedSeconds;
            set => this.RaiseAndSetIfChanged(ref _solverElapsedSeconds, value);
        }

        private bool _isBusyVisualizing;
        public bool IsBusyVisualizing
        {
            get => _isBusyVisualizing;
            set => this.RaiseAndSetIfChanged(ref _isBusyVisualizing, value);
        }

        private string _solverStatusText = "";
        public string SolverStatusText
        {
            get => _solverStatusText;
            set => this.RaiseAndSetIfChanged(ref _solverStatusText, value);
        }

        private static readonly IBrush NormalStatusTextColor = Brush.Parse("yellow");
        private static readonly IBrush SuccessStatusTextColor = Brush.Parse("lime");
        private static readonly IBrush FailedStatusTextColor = Brush.Parse("red");

        private IBrush _solverStatusTextColor = NormalStatusTextColor;
        public IBrush SolverStatusTextColor
        {
            get => _solverStatusTextColor;
            set => this.RaiseAndSetIfChanged(ref _solverStatusTextColor, value);
        }

        private SolveUiState _solveUiState = ViewModels.SolveUiState.Uninitialized;
        private readonly ISolveSettingsManager _settingsManager;

        public SolveUiState SolveUiState
        {
            get => _solveUiState;
            set
            {
                this.RaiseAndSetIfChanged(ref _solveUiState, value);
                RefreshStateDependentUiFlags();
            }
        }

        private SolutionGridModel _solutionGridModel;
        private readonly IVerboseMemoryLogger _verboseLogger;

        public SolutionGridModel SolutionGridModel
        {
            get => _solutionGridModel;
            set
            {
                this.RaiseAndSetIfChanged(ref _solutionGridModel, value);
                this.RaisePropertyChanged(nameof(SolutionGridModels));
            }
        }

        public SolutionGridModel[] SolutionGridModels =>
            SolutionGridModel != null ? new SolutionGridModel[] { SolutionGridModel } : Array.Empty<SolutionGridModel>();

        private IReadOnlyList<string> _solverLog;
        private readonly IVisualizer _visualizer;

        public IReadOnlyList<string> SolverLog
        {
            get => _solverLog;
            set => this.RaiseAndSetIfChanged(ref _solverLog, value);
        }

        private bool _toggleGridVisualization = false;
        public bool ToggleGridVisualization
        {
            get => _toggleGridVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleGridVisualization, value);
        }

        private bool _toggleCrosshairVisualization;
        public bool ToggleCrosshairVisualization
        {
            get => _toggleCrosshairVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleCrosshairVisualization, value);
        }

        private bool _toggleDetectedStarsVisualization;
        public bool ToggleDetectedStarsVisualization
        {
            get => _toggleDetectedStarsVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleDetectedStarsVisualization, value);
        }

        private bool _toggleMatchingQuadsVisualization;
        public bool ToggleMatchingQuadsVisualization
        {
            get => _toggleMatchingQuadsVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleMatchingQuadsVisualization, value);
        }

        private bool _toggleFormedQuadsVisualization;
        public bool ToggleFormedQuadsVisualization
        {
            get => _toggleFormedQuadsVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleFormedQuadsVisualization, value);
        }

        private bool _toggleDsoVisualization;
        private readonly IDsoDatabase _dsoDatabase;

        public bool ToggleDsoVisualization
        {
            get => _toggleDsoVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleDsoVisualization, value);
        }

        private bool _toggleStretchLevelsVisualization;
        public bool ToggleStretchLevelsVisualization
        {
            get => _toggleStretchLevelsVisualization;
            set => this.RaiseAndSetIfChanged(ref _toggleStretchLevelsVisualization, value);
        }

        private void RefreshStateDependentUiFlags()
        {
            this.RaisePropertyChanged(nameof(ImageInfoLabel));
            this.RaisePropertyChanged(nameof(IsSolving));
            this.RaisePropertyChanged(nameof(ToolbarButtonsEnabled));
            this.RaisePropertyChanged(nameof(OpenImageButtonEnabled));
            this.RaisePropertyChanged(nameof(SolveButtonEnabled));
            this.RaisePropertyChanged(nameof(CancelSolveButtonEnabled));
            this.RaisePropertyChanged(nameof(PlaceHolderTextsVisible));
            this.RaisePropertyChanged(nameof(SaveResultsButtonsEnabled));
        }

        private void ResetVisualizationToggles()
        {
            ToggleCrosshairVisualization = false;
            ToggleDetectedStarsVisualization = false;
            ToggleMatchingQuadsVisualization = false;
            ToggleGridVisualization = false;
            ToggleDsoVisualization = false;
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
            SolutionGridModel = new SolutionGridModel()
            {
                Dec = 23.45678901234,
                Ra = 23.54646467,
                FieldRadius = 1.54646775,
                Orientation = 45.6786867867,
                Parity = "Normal"
            };
            SolverLog = new string[]
            {
                "Line 1",
                "Line 2"
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
            _settingsManager = serviceProvider.GetService<ISolveSettingsManager>();
            _verboseLogger = serviceProvider.GetService<IVerboseMemoryLogger>();
            _visualizer = serviceProvider.GetService<IVisualizer>();
            _dsoDatabase = serviceProvider.GetService<IDsoDatabase>();
            Initialize();
        }
        
        private void Initialize()
        {
            PlaceholderImage = new Bitmap(_assetManager.Open(new Uri("avares://WatneyAstrometry.SolverVizTools/Assets/placeholder.jpg")));
            //PlaceholderImage = new Bitmap("z:\\firefox_61OAoUIBsW2.jpg");
            SolverImage = PlaceholderImage;
        }


        public async Task StretchLevels()
        {
            await GenerateVisualizationImage();
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
                await OpenImage(filename);
            }

        }

        public async Task OpenImage(string filename)
        {
            SolveUiState = SolveUiState.ImageOpening;

            try
            {
                var imageData = await _imageManager.LoadImage(filename);
                SolverImage = imageData.UiImage;
                SolverImageData = imageData;

                ImageInfoLabel = $"{imageData.FileName} ({imageData.Width}x{imageData.Height})";
                SolveUiState = SolveUiState.ImageLoaded;
                SolutionGridModel = null;
                ResetVisualizationToggles();
            }
            catch (Exception e)
            {
                await _dialogProvider.ShowMessageBox(OwnerWindow, "Error opening image", $"Failed to open image: {e.Message}");
                SolveUiState = SolveUiState.Uninitialized;
            }

            SolverStatusText = "";
        }

        private void StartCounterUpdate(Stopwatch sw, CancellationToken token)
        {
            _counterTask = Task.Run(async () =>
            {
                while (IsSolving && !token.IsCancellationRequested)
                {
                    await Task.Delay(33);
                    await Dispatcher.UIThread.InvokeAsync(() => SolverElapsedSeconds = $"{sw.Elapsed.TotalSeconds:F1}");
                }
            });
        }

        public async Task CancelSolve()
        {
            _cancellationTokenSource?.Cancel();
        }

        private bool IsDatabaseAvailable()
        {
            var currentConfig = _settingsManager.GetWatneyConfiguration(false, false);
            if (!currentConfig.IsValidQuadDatabasePath)
                return false;
            
            return true;
        }

        public async Task StartSolve()
        {

            if (!IsDatabaseAvailable())
            {
                await _dialogProvider.ShowMessageBox(OwnerWindow, "Error",
                    "Cannot start solve, quad database path is not configured. Check the Settings Manager.",
                    DialogIcon.Error);
                return;
            }

            _solveResult = null;
            SolveUiState = SolveUiState.Solving;
            SolverStatusText = "";
            SolverElapsedSeconds = "0.0";
            SolverStatusTextColor = NormalStatusTextColor;
            _verboseLogger.Clear();
            _starDetectionStopwatch = new Stopwatch();
            _solverStopwatch = new Stopwatch();
            _fullSolveProcessStopwatch = new Stopwatch();

            SolverLog = new string[0];
            await Task.Yield();

            Stopwatch visualCounterStopwatch = Stopwatch.StartNew();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            StartCounterUpdate(visualCounterStopwatch, cancellationToken);

            try
            {

                var currentConfig = _settingsManager.GetWatneyConfiguration(false, false);
                _fullSolveProcessStopwatch.Start();

                if (_solverInstance == null)
                {
                    _solverInstance = new Solver(_verboseLogger);
                    _solverInstance.OnSolveProgress += OnSolveProgress;
                }

                if (_quadDatabaseInstance == null || _quadDatabaseInstance.DatabaseDirectory != currentConfig.QuadDatabasePath)
                {
                    _quadDatabaseInstance?.Dispose();

                    // Note: if the database location changes, we need to re-create the db instance todo
                    _quadDatabaseInstance = new CompactQuadDatabase();
                    _quadDatabaseInstance.UseDataSource(_settingsManager.GetWatneyConfiguration(false, false)
                        .QuadDatabasePath);
                    _solverInstance.UseQuadDatabase(_quadDatabaseInstance);
                }

                // Construct the strategy

                var settingsPane = _serviceProvider.GetService<SettingsPaneViewModel>();
                var profile = settingsPane.SelectedPresetProfile;

                if (profile == null)
                    throw new Exception("Solve profile was null");

                ISearchStrategy strategy = null;
                if (profile.ProfileType == SolveProfileType.Blind)
                {
                    strategy = new BlindSearchStrategy(new BlindSearchStrategyOptions
                    {
                        MaxNegativeDensityOffset = (uint)profile.GenericOptions.LowerDensityOffset,
                        MaxPositiveDensityOffset = (uint)profile.GenericOptions.HigherDensityOffset,
                        MinRadiusDegrees = profile.BlindOptions.MinRadius ?? 0.1,
                        StartRadiusDegrees = profile.BlindOptions.MaxRadius ?? 8,
                        SearchOrderDec = profile.BlindOptions.SearchOrder == SearchOrder.NorthFirst
                            ? BlindSearchStrategyOptions.DecSearchOrder.NorthFirst
                            : BlindSearchStrategyOptions.DecSearchOrder.SouthFirst,
                        UseParallelism = true // todo parameterize this
                    });
                }
                else
                {
                    var opts = new NearbySearchStrategyOptions
                    {
                        UseParallelism = true,
                        //MaxFieldRadiusDegrees = profile.NearbyOptions.FieldRadiusMax, // todo support ranges
                        //MinFieldRadiusDegrees = profile.NearbyOptions.FieldRadiusMin,
                        MaxFieldRadiusDegrees = profile.NearbyOptions.FieldRadius,
                        MinFieldRadiusDegrees = profile.NearbyOptions.FieldRadius,
                        SearchAreaRadiusDegrees = profile.NearbyOptions.SearchRadius,
                        MaxNegativeDensityOffset = (uint)profile.GenericOptions.LowerDensityOffset,
                        MaxPositiveDensityOffset = (uint)profile.GenericOptions.HigherDensityOffset
                    };

                    if (profile.NearbyOptions.InputSource == InputSource.FitsHeaders)
                    {
                        if (_solverImageData.WatneyImage is FitsImage fits)
                        {
                            var centerPos = fits.Metadata.CenterPos;
                            if (centerPos == null)
                                throw new Exception(
                                    "FITS headers set as source for initial coordinate, but no initial coordinate was available");
                            opts.MaxFieldRadiusDegrees = fits.Metadata.ViewSize.DiameterDeg * 0.5;
                            opts.MinFieldRadiusDegrees = fits.Metadata.ViewSize.DiameterDeg * 0.5;
                            strategy = new NearbySearchStrategy(centerPos, opts);
                        }
                        else
                        {
                            throw new Exception("Could not get initial position from FITS headers, because the image is not a FITS file");
                        }
                    }
                    else
                    {
                        var centerPos = ParseCoordsFromProfileRaDec(profile);
                        if (centerPos == null)
                            throw new Exception(
                                "Could not parse valid initial coordinates from profile RA and Dec");
                        strategy = new NearbySearchStrategy(centerPos, opts);
                    }

                }

                var solverOpts = new SolverOptions()
                {
                    UseMaxStars = profile.GenericOptions.MaxStars,
                    UseSampling = profile.GenericOptions.Sampling
                };

                // todo event listeners
                var result = await _solverInstance.SolveFieldAsync(_solverImageData.WatneyImage, strategy, solverOpts, cancellationToken);
                _solveResult = result;

                _fullSolveProcessStopwatch.Stop();

                if (result.Success)
                {
                    SolutionGridModel = new SolutionGridModel
                    {
                        Ra = result.Solution.PlateCenter.Ra,
                        Dec = result.Solution.PlateCenter.Dec,
                        FieldRadius = result.Solution.Radius,
                        Orientation = result.Solution.Orientation,
                        Parity = result.Solution.Parity.ToString(),
                        StarsDetected = result.StarsDetected,
                        StarsUsed = result.StarsUsedInSolve,
                        StarDetectionDuration = _starDetectionStopwatch.Elapsed.TotalSeconds,
                        SolverDuration = _solverStopwatch.Elapsed.TotalSeconds,
                        FullDuration = _fullSolveProcessStopwatch.Elapsed.TotalSeconds,
                        Matches = result.MatchedQuads
                    };

                    SolveUiState = SolveUiState.SolveCompleteSuccess;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SolverStatusText = "Solved successfully!";
                        SolverStatusTextColor = SuccessStatusTextColor;
                    });
                }
                else
                {
                    SolutionGridModel = null;
                    SolveUiState = SolveUiState.SolveCompleteFailure;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SolverStatusText = "Solve was unsuccessful!";
                        SolverStatusTextColor = FailedStatusTextColor;
                    });
                }

                
                SolverLog = _verboseLogger.FullLog;

                _cancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                _solveResult = null;
                SolveUiState = SolveUiState.SolveCompleteFailure;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SolverStatusText = "Error!";
                    SolverStatusTextColor = FailedStatusTextColor;
                });
                _cancellationTokenSource.Cancel();
                _verboseLogger.WriteError($"Solver failed due to an error: {e.Message}");
                _verboseLogger.WriteError($"Stacktrace: {e.StackTrace}");
                SolverLog = _verboseLogger.FullLog;
            }

            _fullSolveProcessStopwatch.Reset();
            _solverStopwatch.Reset();
            _starDetectionStopwatch.Reset();

        }

        public async Task SaveLogToDiskViaDialog()
        {
            var filename = await _dialogProvider.ShowSaveFileDialog(OwnerWindow, "Save log file as...", 
                null, "watney_log.txt", ".txt");
            if(filename != null)
                SaveLogToDisk(filename);
        }

        public void SaveLogToDisk(string filename)
        {
            File.WriteAllLines(filename, SolverLog ?? new List<string>());
        }

        public async Task SaveSolutionWcsToDiskViaDialog()
        {
            var filename = await _dialogProvider.ShowSaveFileDialog(OwnerWindow, "Save WCS file as...",
                null, "watney_solution.wcs", ".wcs");
            if (filename != null)
                SaveSolutionWcsToDisk(filename);
        }

        public void SaveSolutionWcsToDisk(string filename)
        {
            using FileStream fs = new FileStream(filename, FileMode.Create);
            var wcsWriter = new WcsFitsWriter(fs);
            wcsWriter.WriteWcsFile(_solveResult.Solution.FitsHeaders, _solveResult.Solution.ImageWidth, _solveResult.Solution.ImageHeight);
        }

        public async Task SaveSolutionJsonToDiskViaDialog()
        {
            var filename = await _dialogProvider.ShowSaveFileDialog(OwnerWindow, "Save JSON file as...",
                null, "watney_solution.json", ".json");
            if (filename != null)
                SaveSolutionJsonToDisk(filename);
        }

        public void SaveSolutionJsonToDisk(string filename)
        {
            var outputData = new Dictionary<string, object>();
        
            outputData.Add("ra", _solveResult.Solution.PlateCenter.Ra);
            outputData.Add("dec", _solveResult.Solution.PlateCenter.Dec);
            outputData.Add("ra_hms", Conversions.RaDegreesToHhMmSs(_solveResult.Solution.PlateCenter.Ra));
            outputData.Add("dec_dms", Conversions.DecDegreesToDdMmSs(_solveResult.Solution.PlateCenter.Dec));
            outputData.Add("fieldRadius", _solveResult.Solution.Radius);
            outputData.Add("orientation", _solveResult.Solution.Orientation);
            outputData.Add("pixScale", _solveResult.Solution.PixelScale);
            outputData.Add("parity", _solveResult.Solution.Parity.ToString().ToLowerInvariant());

            outputData.Add("starsDetected", _solveResult.StarsDetected);
            outputData.Add("starsUsed", _solveResult.StarsUsedInSolve);
            outputData.Add("timeSpent", _solveResult.TimeSpent.ToString());
            outputData.Add("searchIterations", _solveResult.AreasSearched);

            outputData.Add("imageWidth", _solveResult.Solution.ImageWidth);
            outputData.Add("imageHeight", _solveResult.Solution.ImageHeight);
            outputData.Add("searchRunCenter", _solveResult.SearchRun.Center.ToString());
            outputData.Add("searchRunRadius", _solveResult.SearchRun.RadiusDegrees);
            outputData.Add("quadMatches", _solveResult.MatchedQuads);
            outputData.Add("fieldWidth", _solveResult.Solution.FieldWidth);
            outputData.Add("fieldHeight", _solveResult.Solution.FieldHeight);
            outputData.Add("fits_cd1_1", _solveResult.Solution.FitsHeaders.CD1_1);
            outputData.Add("fits_cd1_2", _solveResult.Solution.FitsHeaders.CD1_2);
            outputData.Add("fits_cd2_1", _solveResult.Solution.FitsHeaders.CD2_1);
            outputData.Add("fits_cd2_2", _solveResult.Solution.FitsHeaders.CD2_2);
            outputData.Add("fits_cdelt1", _solveResult.Solution.FitsHeaders.CDELT1);
            outputData.Add("fits_cdelt2", _solveResult.Solution.FitsHeaders.CDELT2);
            outputData.Add("fits_crota1", _solveResult.Solution.FitsHeaders.CROTA1);
            outputData.Add("fits_crota2", _solveResult.Solution.FitsHeaders.CROTA2);
            outputData.Add("fits_crpix1", _solveResult.Solution.FitsHeaders.CRPIX1);
            outputData.Add("fits_crpix2", _solveResult.Solution.FitsHeaders.CRPIX2);
            outputData.Add("fits_crval1", _solveResult.Solution.FitsHeaders.CRVAL1);
            outputData.Add("fits_crval2", _solveResult.Solution.FitsHeaders.CRVAL2);

            var json = JsonSerializer.Serialize(outputData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filename, json);
            
        }

        private void OnSolveProgress(SolverStep step)
        {
            
            if (step == SolverStep.ImageReadStarted)
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Reading image...");

            if (step == SolverStep.ImageReadFinished)
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Image read finished");

            if (step == SolverStep.QuadFormationStarted)
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Forming quads from stars...");

            if (step == SolverStep.QuadFormationFinished)
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Solver is running...");

            if (step == SolverStep.SolveProcessStarted)
            {
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Solving image...");
                _solverStopwatch.Start();
            }

            if (step == SolverStep.SolveProcessFinished)
            {
                _solverStopwatch.Stop();
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Solver finished");
            }

            if (step == SolverStep.StarDetectionStarted)
            {
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Detecting stars...");
                _starDetectionStopwatch.Start();
            }


            if (step == SolverStep.StarDetectionFinished)
            {
                _starDetectionStopwatch.Stop();
                Dispatcher.UIThread.InvokeAsync(() => SolverStatusText = "Star detection finished");
            }
                
        }

        private EquatorialCoords ParseCoordsFromProfileRaDec(SolveProfile profile)
        {
            var ra = profile.NearbyOptions.Ra;
            var dec = profile.NearbyOptions.Dec;

            // Decimal numbers
            var raRegex = new Regex(@"^[-+]{0,1}\d+\.{0,1}\d*$");
            var decRegex = new Regex(@"^\d+\.{0,1}\d*$");

            if (raRegex.IsMatch(ra) && decRegex.IsMatch(dec))
            {
                try
                {
                    var raVal = double.Parse(ra, CultureInfo.InvariantCulture);
                    var decVal = double.Parse(dec, CultureInfo.InvariantCulture);
                    return new EquatorialCoords(raVal, decVal);
                }
                catch (Exception e)
                {
                    _dialogProvider.ShowMessageBox(OwnerWindow, "Error parsing RA/Dec coordinates",
                        $"Cannot use given RA/Dec coordinates: {e.Message}");
                }
            }
            else
            {
                try
                {
                    var raVal = Conversions.RaToDecimal(ra);
                    var decVal = Conversions.DecToDecimal(dec);
                    return new EquatorialCoords(raVal, decVal);
                }
                catch (Exception e)
                {
                    _dialogProvider.ShowMessageBox(OwnerWindow, "Error parsing RA/Dec coordinates",
                        $"Cannot use given RA/Dec coordinates: {e.Message}");
                }
            }

            return null;
        }

        public async Task ShowDeepSkyObjects()
        {
            IsBusyVisualizing = true;
            RefreshStateDependentUiFlags();

            if (!_dsoDatabase.IsLoaded)
            {
                if (!_dsoDatabase.HasDatabaseFileDownloaded)
                {
                    var downloadDialog = _viewProvider.Instantiate<DsoDatabaseDownloadViewModel>();
                    downloadDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    var databaseFilename = await downloadDialog.ShowDialog<string>(OwnerWindow);
                    if (databaseFilename != null)
                    {
                        await _dsoDatabase.Load(databaseFilename);
                    }
                    else
                    {
                        ToggleDsoVisualization = false;
                        return;
                    }
                }
                else
                {
                    await _dsoDatabase.Load();
                }
            }

            await GenerateVisualizationImage();
        }

        public async Task GenerateVisualizationImage()
        {
            var flags = VisualizationModes.None;
            IsBusyVisualizing = true;
            RefreshStateDependentUiFlags();

            if (ToggleStretchLevelsVisualization)
                flags |= VisualizationModes.StretchLevels;

            if (ToggleFormedQuadsVisualization)
                flags |= VisualizationModes.FormedQuads;

            if (ToggleCrosshairVisualization)
                flags |= VisualizationModes.Crosshair;

            if (ToggleDetectedStarsVisualization)
                flags |= VisualizationModes.DetectedStars;

            if (ToggleDsoVisualization)
                flags |= VisualizationModes.DeepSkyObjects;

            if (ToggleGridVisualization)
                flags |= VisualizationModes.Grid;

            if (ToggleMatchingQuadsVisualization)
                flags |= VisualizationModes.QuadMatches;
            
            SolverImage = await _visualizer.BuildVisualization(_solverImageData.EditableImage, _solveResult, flags);

            IsBusyVisualizing = false;
            RefreshStateDependentUiFlags();
        }

    }
}
