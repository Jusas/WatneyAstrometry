using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.GaiaStarExtractor
{
    /// <summary>
    /// A simple star data extractor that only extracts bare bones data
    /// from the Gaia2 data set into SkySegmentSphere cells. We can then derive different
    /// data sets from this subset a lot faster.
    /// </summary>
    class Program
    {
        public class Opts
        {
            [Option('m', "max-magnitude", HelpText = "Maximum magnitude of stars to include", Default = 17.0)]
            public double MaxMag { get; set; }
            
            [Option('o', "out", Required = true, HelpText = "Output directory")]
            public string OutputPath { get; set; }

            [Option('f', "files", Required = true, HelpText = "Gaia .csv.gz file directory")]
            public string FileDir { get; set; }
            
            [Option('t', "threads", Required = false, HelpText = "Threads to use. Defaults to detected logical processor count count - 1 (or 1 if only one is detected)")]
            public int Threads { get; set; } = -1;
        }

        private static Dictionary<string, Stream> _cachedOutputStreams = new Dictionary<string, Stream>();
        private static Dictionary<string, object> _streamLocks = new Dictionary<string, object>();

        static async Task Run(Opts options)
        {

            var concurrentOps = options.Threads < 0 ? Math.Max(Environment.ProcessorCount - 1, 1) : options.Threads;
            var gaiaFiles = Directory.GetFiles(options.FileDir, "*.csv.gz");

            var outDir = options.OutputPath;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            Console.WriteLine($"Found {gaiaFiles.Length} .csv.gz files");
            
            long totalExtracted = 0;
            long filesProcessed = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int[] magnitudes = new int[20];

            InitializeOutputStreams(options);

            Parallel.ForEach(gaiaFiles, new ParallelOptions() { MaxDegreeOfParallelism = concurrentOps },
                (filename, state, idx) =>
                {
                    using (var sourceStream = File.OpenRead(filename))
                    {
                        using (var memStream = new MemoryStream())
                        using (var gzStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                        {
                            gzStream.CopyTo(memStream);
                            memStream.Seek(0, SeekOrigin.Begin);

                            var (extracted, mags) = ExtractData(memStream, options.MaxMag);
                            for (var i = 0; i < magnitudes.Length; i++)
                                Interlocked.Add(ref magnitudes[i], mags[i]);

                            Interlocked.Add(ref totalExtracted, extracted);
                            Interlocked.Increment(ref filesProcessed);
                        }
                    }
                    Console.WriteLine($"Files {filesProcessed}/{gaiaFiles.Length} processed, stars collected: {totalExtracted}");
                });
            
            CloseOutputStreams();
            sw.Stop();

            Console.WriteLine($"Completed, extraction took {sw.Elapsed}");
            
            var stats = magnitudes.Select((m, i) => $"Magnitude {(i == 0 ? "<" : i.ToString("##"))}-{(i + 1):##} star count: {m}")
                .Append($"Total stars: {totalExtracted}")
                .Append($"Extraction took {sw.Elapsed}");
            File.WriteAllLines(Path.Combine(outDir, "stats.txt"), stats);
        }

        private static void InitializeOutputStreams(Opts options)
        {
            if (!_cachedOutputStreams.Any())
            {
                foreach (var cell in SkySegmentSphere.Cells)
                {
                    var stream = new FileStream(Path.Combine(options.OutputPath, $"gaia2_stars-{cell.CellId}.stars"),
                        FileMode.Create);
                    _cachedOutputStreams.Add(cell.CellId, stream);
                    _streamLocks.Add(cell.CellId, new object());
                }
            }
        }

        private static void CloseOutputStreams()
        {
            foreach (var stream in _cachedOutputStreams.Values)
            {
                stream.Close();
            }
        }


        private static readonly object _mutex = new object();

        static (int extracted, int[] magnitudes) ExtractData(Stream data, double maxMag)
        {
            int total = 0;
            int[] magnitudes = new int[20];


            var tmpStreams = SkySegmentSphere.Cells
                .Select(x => x.CellId)
                .ToDictionary(x => x, x => new MemoryStream());
            
            using (var reader = new StreamReader(data, Encoding.ASCII))
            {
                string line = reader.ReadLine(); // header line
                while ((line = reader.ReadLine()) != null)
                {

                    var elems = line.Split(",");

                    // Interesting fields:
                    // 2: source id (long/string)
                    // 4: ref epoch (double)
                    // 5: ra (double)
                    // 7: dec (double)
                    // 50: phot_g_mean_mag (float)

                    var mag = float.Parse(elems[50], CultureInfo.InvariantCulture);

                    if (mag >= maxMag)
                        continue;

                    var ra = double.Parse(elems[5], CultureInfo.InvariantCulture);
                    var dec = double.Parse(elems[7], CultureInfo.InvariantCulture);

                    var raBytes = BitConverter.GetBytes(ra);
                    var decBytes = BitConverter.GetBytes(dec);
                    var gMagBytes = BitConverter.GetBytes(mag); // A byte could host mags up to 25.5, with one decimal accuracy...

                    magnitudes[mag < 0 ? 0 : (byte) mag] += 1;

                    var bytes = raBytes
                        .Concat(decBytes)
                        .Concat(gMagBytes)
                        .ToArray();

                    var cell = SkySegmentSphere.GetCellAt(new EquatorialCoords(ra, dec));
                    
                    tmpStreams[cell.CellId].Write(bytes);
                    total++;
                }
            }

            foreach (var tmpStream in tmpStreams)
            {
                var cellId = tmpStream.Key;
                if (tmpStream.Value.Length == 0)
                {
                    tmpStream.Value.Dispose();
                    continue;
                }

                lock (_streamLocks[cellId])
                {
                    tmpStream.Value.Seek(0, SeekOrigin.Begin);
                    tmpStream.Value.CopyTo(_cachedOutputStreams[cellId]);
                }
            }
            

            return (total, magnitudes);
        }

        static async Task Main(string[] args)
        {

            await CommandLine.Parser.Default.ParseArguments<Opts>(args)
                .WithParsedAsync(Run);


        }
    }
}
