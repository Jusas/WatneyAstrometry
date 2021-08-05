using System;
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
            var img = r.FromFile("Resources/fits/ngc1491.fits"); 
            
            var blindStrategy = new BlindSearchStrategy(new BlindSearchStrategy.Options()
            {
                SearchOrderRa = BlindSearchStrategy.RaSearchOrder.EastFirst,
                SearchOrderDec = BlindSearchStrategy.DecSearchOrder.NorthFirst,
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
                UseSampling = 4
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
            var img = r.FromFile("Resources/fits/m31.fits");
            
            var nearCenter = new EquatorialCoords(img.Metadata.CenterPos.Ra + 0.1, img.Metadata.CenterPos.Dec + 5);
            var nearbyStrategy = new NearbySearchStrategy(nearCenter, new NearbySearchStrategy.Options()
            {
                ScopeFieldRadius = 2,
                SearchAreaRadius = 10,
                MaxNegativeDensityOffset = 2,
                MaxPositiveDensityOffset = 2
            });
            
            var solver = new Solver().UseQuadDatabase(() =>
                new CompactQuadDatabase().UseDataSource(_quadDbPath, false));
            
            var token = CancellationToken.None;
            var options = new SolverOptions()
            {
                UseSampling = 4
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
                visualization.VisualizeStars(solveResult.DetectedStars);
                visualization.DrawImageQuadsFromMatches(solveResult.MatchInstances);
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
                await QuadDatabaseVisualizer.VisualizeAreaAndFormQuads(quadDb, image, solveResult.DetectedQuadDensity, solveResult.Solution.PlateCenter, pixSize, focalLen);
                image.SaveAsPng($"{nameof(Visualize_database_quads_in_solve)}.png");
            }
            
            
        }


    }
}