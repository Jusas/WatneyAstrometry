using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VizUtils;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.StarDetection;
using WatneyAstrometry.Core.Tests.Utils;
using WatneyAstrometry.Core.Types;
using Xunit;
using Xunit.Abstractions;

namespace WatneyAstrometry.Core.Tests
{
    public class SolverTests
    {
        private readonly ITestOutputHelper _testOutput;
        private string _quadDbPath;

        public SolverTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;

            // Use quad path from launchSettings.json, alternatively use environment variable.

            var launchSettings = "Properties/launchSettings.json";
            if (File.Exists(launchSettings))
            {
                var settingJson = JObject.Parse(File.ReadAllText(launchSettings));
                var envVars = settingJson["profiles"]?["WatneyAstrometry.Core.Tests"]?["environmentVariables"];
                if (envVars != null && envVars["SOLVERTESTS_QUADDB_PATH"] != null)
                {
                    _quadDbPath = envVars["SOLVERTESTS_QUADDB_PATH"].ToString();
                }
            }

            if (Environment.GetEnvironmentVariable("SOLVERTESTS_QUADDB_PATH") != null)
                _quadDbPath = Environment.GetEnvironmentVariable("SOLVERTESTS_QUADDB_PATH");
        }
        

        [Fact]
        public async Task Should_successfully_blind_solve()
        {
            DefaultFitsReader r = new DefaultFitsReader();
            var img = r.FromFile("Resources/fits/m81.fits"); 
            
            var blindStrategy = new BlindSearchStrategy(new BlindSearchStrategyOptions()
            {
                SearchOrderRa = BlindSearchStrategyOptions.RaSearchOrder.EastFirst,
                SearchOrderDec = BlindSearchStrategyOptions.DecSearchOrder.NorthFirst,
                MinRadiusDegrees = 0.5f,
                StartRadiusDegrees = 8,
                MaxNegativeDensityOffset = 1,
                MaxPositiveDensityOffset = 1,
                UseParallelism = true
            });

            SolverUnitTestVerboseLogger testLogger = null; // new SolverUnitTestVerboseLogger(_testOutput);
            
            var solver = new Solver(testLogger).UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));
            
            var token = CancellationToken.None;
            var options = new SolverOptions()
            {
                UseSampling = 24,
                UseMaxStars = 300
            };
            var solveResult = await solver.SolveFieldAsync(img, blindStrategy, options, token);

            if (solveResult.Success)
            {
                _testOutput.WriteLine(string.Join(" / ",
                    Conversions.DegreesToHourAngles(solveResult.Solution.PlateCenter)));
                _testOutput.WriteLine("Matches: " + solveResult.MatchedQuads);
                _testOutput.WriteLine("Search iteration - radius: " + solveResult.SearchRun.RadiusDegrees);
                _testOutput.WriteLine("Search iteration - center: " + string.Join(" / ", Conversions.DegreesToHourAngles(solveResult.SearchRun.Center)));
                _testOutput.WriteLine(solveResult.TimeSpent.ToString());
            }
            else
            {
                _testOutput.WriteLine("Failed");
            }
            solveResult.Success.Should().BeTrue();


        }

        [Fact]
        public async Task Should_successfully_nearby_solve()
        {
            DefaultFitsReader r = new DefaultFitsReader();
            var img = r.FromFile("Resources/fits/ngc7331.fits");
            
            //var nearCenter = new EquatorialCoords(img.Metadata.CenterPos.Ra + 0.1, img.Metadata.CenterPos.Dec + 5);
            var nearCenter = EquatorialCoords.FromText("22 36 30", "34 15 43");
            var nearbyStrategy = new NearbySearchStrategy(nearCenter, new NearbySearchStrategyOptions()
            {
                ScopeFieldRadius = 1.0f,
                SearchAreaRadius = 10,
                MaxNegativeDensityOffset = 1,
                MaxPositiveDensityOffset = 1
            });
            
            var solver = new Solver().UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));
            
            var token = CancellationToken.None;
            var options = new SolverOptions()
            {
                //UseMaxStars = 100,
                UseSampling = 1
            };
            var solveResult = await solver.SolveFieldAsync(img, nearbyStrategy, options, token);

            if (solveResult.Success)
            {
                _testOutput.WriteLine(string.Join(" / ",
                    Conversions.DegreesToHourAngles(solveResult.Solution.PlateCenter)));
                _testOutput.WriteLine("Matches: " + solveResult.MatchedQuads);
                _testOutput.WriteLine("Search iteration - radius: " + solveResult.SearchRun.RadiusDegrees);
                _testOutput.WriteLine("Search iteration - center: " + string.Join(" / ", Conversions.DegreesToHourAngles(solveResult.SearchRun.Center)));
                _testOutput.WriteLine(solveResult.TimeSpent.ToString());
            }
            else
            {
                _testOutput.WriteLine("Failed");
            }
            solveResult.Success.Should().BeTrue();


        }


        [Fact]
        [Trait("Category", "Visual")]
        public async Task Visualize_matches_in_solve()
        {
            DefaultFitsReader r = new DefaultFitsReader();
            var img = r.FromFile("Resources/fits/ngc7331.fits");

            var center = new EquatorialCoords(img.Metadata.CenterPos.Ra, img.Metadata.CenterPos.Dec);
            var pointStrategy = new PointSearchStrategy(center, new PointSearchStrategy.Options()
            {
                MaxNegativeDensityOffset = 1,
                MaxPositiveDensityOffset = 1,
                RadiusDegrees = (float) img.Metadata.ViewSize.DiameterDeg * 0.5f
            });

            var solver = new Solver().UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));

            var token = CancellationToken.None;
            var options = new SolverOptions();
            var solveResult = await solver.SolveFieldAsync(img, pointStrategy, options, token);

            solveResult.Success.Should().BeTrue();
            
            using (var visualization = TestImageUtils.FitsImagePixelBufferToRgbaImage(img as FitsImage))
            {
                visualization.VisualizeStars(solveResult.DiagnosticsData.DetectedStars);
                visualization.DrawImageQuadsFromMatches(solveResult.DiagnosticsData.MatchInstances);
                visualization.SaveAsPng($"{nameof(Visualize_matches_in_solve)}.png");
            }
        }

        [Fact]
        [Trait("Category", "Visual")]
        public async Task Visualize_database_quads_in_solve()
        {
            DefaultFitsReader r = new DefaultFitsReader();
            var img = r.FromFile("Resources/fits/ngc7331.fits");

            var center = new EquatorialCoords(img.Metadata.CenterPos.Ra, img.Metadata.CenterPos.Dec);
            var pointStrategy = new PointSearchStrategy(center, new PointSearchStrategy.Options()
            {
                MaxNegativeDensityOffset = 1,
                MaxPositiveDensityOffset = 1,
                RadiusDegrees = (float)img.Metadata.ViewSize.DiameterDeg * 0.5f
            });

            var solver = new Solver().UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));

            var token = CancellationToken.None;
            var options = new SolverOptions();
            var solveResult = await solver.SolveFieldAsync(img, pointStrategy, options, token);

            solveResult.Success.Should().BeTrue();

            

            var fitsImage = (FitsImage) img;
            var pixSize = fitsImage.HduHeaderRecords.First(x => x.Keyword == "PIXSIZE1").ValueAsDouble;
            var focalLen = fitsImage.HduHeaderRecords.First(x => x.Keyword == "FOCALLEN").ValueAsDouble;
            
            var quadDb = new CompactQuadDatabase().UseDataSource(_quadDbPath, false);

            using (var image = new Image<Rgba32>(img.Metadata.ImageWidth, img.Metadata.ImageHeight))
            {
                await QuadDatabaseVisualizer.VisualizeAreaAndFormQuads(quadDb, image, solveResult.DiagnosticsData.DetectedQuadDensity, solveResult.Solution.PlateCenter, pixSize, focalLen);
                image.SaveAsPng($"{nameof(Visualize_database_quads_in_solve)}.png");
            }
            
            
        }


        [Fact]
        [Trait("Category", "Performance")]
        public async Task Blind_performance_with_sampling()
        {
            DefaultFitsReader r = new DefaultFitsReader();
            //var img = r.FromFile("Resources/fits/ic1795.fits"); 
            //var img = r.FromFile("Resources/fits/m81.fits"); 
            //var img = r.FromFile("Resources/fits/m31.fits"); 
            var img = r.FromFile("Resources/fits/trunk.fit"); 
            //var img = r.FromFile("Resources/fits/ic1936l.fit"); 
            
            var blindStrategy = new BlindSearchStrategy(new BlindSearchStrategyOptions()
            {
                SearchOrderRa = BlindSearchStrategyOptions.RaSearchOrder.EastFirst,
                SearchOrderDec = BlindSearchStrategyOptions.DecSearchOrder.NorthFirst,
                //SearchOrderDec = BlindSearchStrategy.DecSearchOrder.SouthFirst,
                //SearchOrderRa = BlindSearchStrategy.RaSearchOrder.WestFirst,
                MinRadiusDegrees = 0.5f,
                StartRadiusDegrees = 8,
                MaxNegativeDensityOffset = 1,
                MaxPositiveDensityOffset = 1,
                UseParallelism = true
            });
            
            var solver = new Solver().UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));
            
            var token = CancellationToken.None;
            var options = new SolverOptions()
            {
                UseMaxStars = 300,
                UseSampling = 0
            };

            var timeList = new List<string>();
            var areasList = new List<string>();
            var radii = new List<string>();
            var runTypes = new List<string>();

            SolveResult solveResult = null;
            //for (var i = 24; i >= 17; i--)
            for (var i = 1; i <= 32; i++)
            {
                options.UseSampling = i;
                solveResult = await solver.SolveFieldAsync(img, blindStrategy, options, token);
            
                _testOutput.WriteLine($"With sampling = {i}");
                if (solveResult.Success)
                {
                    _testOutput.WriteLine(string.Join(" / ",
                        Conversions.DegreesToHourAngles(solveResult.Solution.PlateCenter)));
                    _testOutput.WriteLine("  Matches: " + solveResult.MatchedQuads);
                    _testOutput.WriteLine("  Search iteration - radius: " + solveResult.SearchRun.RadiusDegrees);
                    _testOutput.WriteLine("  Search iteration - center: " + string.Join(" / ", Conversions.DegreesToHourAngles(solveResult.SearchRun.Center)));
                    _testOutput.WriteLine("  Areas searched: " + solveResult.AreasSearched);
                    _testOutput.WriteLine("  Run type: " + solveResult.DiagnosticsData.FoundUsingRunType.ToString());
                    _testOutput.WriteLine("  " + solveResult.TimeSpent.ToString());
                    timeList.Add(solveResult.TimeSpent.ToString());
                    areasList.Add($"{solveResult.AreasSearched}");
                    radii.Add($"{solveResult.SearchRun.RadiusDegrees}");
                    runTypes.Add(solveResult.DiagnosticsData.FoundUsingRunType.ToString());
                    
                }
                else
                {
                    _testOutput.WriteLine("  Failed");
                    _testOutput.WriteLine("  " + solveResult.TimeSpent.ToString());
                    timeList.Add(solveResult.TimeSpent.ToString());
                }
            }
            
            _testOutput.WriteLine($"Stars used: {solveResult.DiagnosticsData.UsedStarCount}");
            timeList.ForEach(x => _testOutput.WriteLine(x));
            areasList.ForEach(x => _testOutput.WriteLine(x));
            radii.ForEach(x => _testOutput.WriteLine(x));
            runTypes.ForEach(x => _testOutput.WriteLine(x));

        }




    }
}