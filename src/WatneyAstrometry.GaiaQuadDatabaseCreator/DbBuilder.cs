using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator
{
    public class DbBuilder
    {
        private string _sourceFilesDir;
        private QuadDatabaseCellFile[] _quadCells;
        private string _outputDir;
        private int _starsPerSqDeg;
        //private double _starsPerDegree;
        private readonly float _passFactor;
        private readonly int _passes;

        public DbBuilder(Program.Options options)
        {
            //string sourceFilesDir, double fieldSize, int starCount, int passes, double passFactor, string outputDir
            if (!Directory.Exists(options.InputDir))
                throw new Exception("Source directory does not exist");

            _sourceFilesDir = options.InputDir;
            _starsPerSqDeg = options.StarsPerSqDeg;
            _outputDir = options.OutputDir;
            _passes = options.Passes;
            _passFactor = (float) options.PassFactor;

            var files = Directory.GetFiles(_sourceFilesDir, "*.stars");

            var quadCells = new List<QuadDatabaseCellFile>();
            if (string.IsNullOrEmpty(options.SelectedCell))
            {
                foreach (var cell in SkySegmentSphere.Cells)
                {
                    var matchingStarFile = files.FirstOrDefault(x => x.EndsWith($"{cell.CellId}.stars"));
                    if (matchingStarFile == null)
                        throw new Exception($"No star file found for sky sphere segment {cell.CellId}");
                    quadCells.Add(new QuadDatabaseCellFile(cell, options.StarsPerSqDeg, options.PassFactor,
                        options.Passes, matchingStarFile));
                }
            }
            else
            {
                var cell = SkySegmentSphere.Cells.First(x => x.CellId == options.SelectedCell);
                var matchingStarFile = files.FirstOrDefault(x => x.EndsWith($"{cell.CellId}.stars"));
                if (matchingStarFile == null)
                    throw new Exception($"No star file found for sky sphere segment {cell.CellId}");
                quadCells.Add(new QuadDatabaseCellFile(cell, options.StarsPerSqDeg, options.PassFactor, options.Passes,
                    matchingStarFile));

            }

            _quadCells = quadCells.ToArray();
        }

        public void Build(int threads)
        {
            Console.WriteLine($"Building star database cells from source file '{_sourceFilesDir}'.");
            Console.WriteLine($"Working with {threads} threads...");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var starByteSize = (sizeof(double) * 2 + sizeof(float));
            var readBatchCount = 10000;

            var lockObj = new object();
            int progress = 0;
            int max = _quadCells.Length;
            long quadCount = 0;
            
            Parallel.For(0, _quadCells.Length, new ParallelOptions() { MaxDegreeOfParallelism = threads },
                (idx) =>
                {
                    var cellFile = _quadCells[idx];
                    using (var stream = new FileStream(cellFile.StarSourceFile, FileMode.Open, FileAccess.Read))
                    {
                        var sourceStarCount = stream.Length / starByteSize;
                        var batchesToRead = (long)Math.Ceiling(sourceStarCount / (double)readBatchCount);

                        for (var i = 0; i < batchesToRead; i++)
                        {
                            var buf = new byte[readBatchCount * starByteSize];
                            var bytesRead = stream.Read(buf, 0, buf.Length);

                            var starsRead = bytesRead / starByteSize;

                            var advance = 0;
                            for (var j = 0; j < starsRead; j++)
                            {
                                cellFile.AddStar(new QuadDatabaseCellFile.Star
                                {
                                    Ra = (float)BitConverter.ToDouble(buf, advance),
                                    Dec = (float)BitConverter.ToDouble(buf, advance + sizeof(double)),
                                    Mag = (byte)(BitConverter.ToSingle(buf, advance + sizeof(double) * 2) * 10)
                                });

                                advance += starByteSize;
                            }
                        }
                        
                        cellFile.Serialize(_outputDir);
                        _quadCells[idx] = null;

                        Interlocked.Increment(ref progress);
                        Interlocked.Add(ref quadCount, cellFile.QuadCount);
                        lock (lockObj)
                        {
                            Console.WriteLine($"Progress: {progress} / {max}");
                        }

                        cellFile.Dispose();
                    }

                    if (progress % 10 == 0)
                    {
                        Console.WriteLine("Calling garbage collection");
                        GC.Collect();
                    }

                });

            
            //Console.WriteLine($"Serializing into quad cell files using {threads} threads...");
            //Parallel.ForEach(_quadCells, new ParallelOptions() {MaxDegreeOfParallelism = threads},
            //    (cellFile, state, idx) => { cellFile.Serialize(_outputDir, _passes, _passFactor); });

            stopwatch.Stop();

            Console.WriteLine($"Serializing completed in {stopwatch.Elapsed}");

            var statsFile = Path.Combine(_outputDir, $"stats-{_passes}-{_starsPerSqDeg}.txt");
            var stats = new[]
            {
                $"Total formed quads: {quadCount}",
                $"Extraction took {stopwatch.Elapsed}"
            };
            File.WriteAllLines(statsFile, stats);

        }
    }
}