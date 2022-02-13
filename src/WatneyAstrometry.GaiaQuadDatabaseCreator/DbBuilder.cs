// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator
{
    public class DbBuilder
    {
        private string _sourceFilesDir;
        private List<QuadDatabaseCellFile> _quadCells;
        private string _outputDir;
        private int _starsPerSqDeg;
        //private double _starsPerDegree;
        private readonly float _passFactor;
        private readonly int _startPassIndex;
        private readonly int _endPassIndex;

        public const string DataSourcePrefix = "gaia2";

        private QuadDatabaseIndexFile _databaseIndexFile;

        private List<string> _processedCells = new();

        private string BuildStatusFileName => Path.Combine(_outputDir, 
            $"buildstatus-{_startPassIndex:00}-{_endPassIndex:00}-{_starsPerSqDeg}.json");

        private string StatsFileName => Path.Combine(_outputDir,
            $"stats-{_startPassIndex:00}-{_endPassIndex:00}-{_starsPerSqDeg}.txt");

        public DbBuilder(Program.Options options)
        {
            //string sourceFilesDir, double fieldSize, int starCount, int passes, double passFactor, string outputDir
            if (!Directory.Exists(options.InputDir))
                throw new Exception("Source directory does not exist");

            _sourceFilesDir = options.InputDir;
            _starsPerSqDeg = options.StarsPerSqDeg;
            _outputDir = options.OutputDir;
            _startPassIndex = options.StartPass;
            _endPassIndex = options.EndPass;
            _passFactor = (float) options.PassFactor;
            
            var indexFilename = Path.Combine(_outputDir, 
                $"{DataSourcePrefix}-{_startPassIndex:00}-{_endPassIndex:00}-{options.StarsPerSqDeg}.qdbindex");
            _databaseIndexFile = new QuadDatabaseIndexFile(indexFilename, !options.NoResume);

            var files = Directory.GetFiles(_sourceFilesDir, "*.stars");

            var quadCells = new List<QuadDatabaseCellFile>();
            if (string.IsNullOrEmpty(options.SelectedCell))
            {
                foreach (var cell in SkySegmentSphere.Cells)
                {
                    var matchingStarFile = files.FirstOrDefault(x => x.EndsWith($"{cell.CellId}.stars"));
                    if (matchingStarFile == null)
                        throw new Exception($"No star file found for sky sphere segment {cell.CellId}");
                    var cellOutputFilename =
                        $"{DataSourcePrefix}-{cell.CellId}-{_startPassIndex:00}-{_endPassIndex:00}-{options.StarsPerSqDeg}.qdb";
                    quadCells.Add(new QuadDatabaseCellFile(_databaseIndexFile, cell, options.StarsPerSqDeg, options.PassFactor,
                        _startPassIndex, _endPassIndex, matchingStarFile, cellOutputFilename));
                }
            }
            else
            {
                var cell = SkySegmentSphere.Cells.First(x => x.CellId == options.SelectedCell);
                var matchingStarFile = files.FirstOrDefault(x => x.EndsWith($"{cell.CellId}.stars"));
                if (matchingStarFile == null)
                    throw new Exception($"No star file found for sky sphere segment {cell.CellId}");
                var cellOutputFilename =
                    $"{DataSourcePrefix}-{cell.CellId}-{_startPassIndex:00}-{_endPassIndex:00}-{options.StarsPerSqDeg}.qdb";
                quadCells.Add(new QuadDatabaseCellFile(_databaseIndexFile, cell, options.StarsPerSqDeg, options.PassFactor, _startPassIndex, _endPassIndex,
                    matchingStarFile, cellOutputFilename));

            }

            _quadCells = quadCells;

            if (!options.NoResume)
            {
                var alreadyProcessed = ReadStatusFromJson();
                if (alreadyProcessed.Any())
                {
                    _processedCells = alreadyProcessed;
                    _quadCells.RemoveAll(c => alreadyProcessed.Contains(c.CellReference.CellId));
                }
            }

        }

        public void Build(int threads, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Building star database cells from source files in '{_sourceFilesDir}'.");
            Console.WriteLine($"Working with {threads} threads...");

            if(_processedCells.Any())
                Console.WriteLine("Continuing where last stopped, already processed cells: " + string.Join(", ", _processedCells));

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var starByteSize = (sizeof(double) * 2 + sizeof(float));
            var readBatchCount = 10000;

            var lockObj = new object();
            int progress = 0;
            int max = _quadCells.Count;
            long quadCount = 0;

            
            Parallel.For(0, _quadCells.Count, new ParallelOptions() { MaxDegreeOfParallelism = threads },
                (idx) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var cellFile = _quadCells[idx];
                    Console.WriteLine($"{cellFile.CellReference.CellId}: Reading star source to memory...");

                    using (var stream = new FileStream(cellFile.StarSourceFile, FileMode.Open, FileAccess.Read))
                    {
                        var sourceStarCount = stream.Length / starByteSize;
                        var batchesToRead = (long)Math.Ceiling(sourceStarCount / (double)readBatchCount);

                        for (var i = 0; i < batchesToRead; i++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

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

                        if (cancellationToken.IsCancellationRequested)
                            return;

                        cellFile.Serialize(_outputDir);
                        _quadCells[idx] = null;

                        Interlocked.Increment(ref progress);
                        Interlocked.Add(ref quadCount, cellFile.QuadCount);
                        lock (lockObj)
                        {
                            _processedCells.Add(cellFile.CellReference.CellId);
                            SaveStatusAsJson();
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

            

            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
                Console.WriteLine("Cancellation was signaled! Job stopped!");
            else
                Console.WriteLine($"Serializing completed in {stopwatch.Elapsed}");
            
            var stats = new[]
            {
                $"Total formed quads: {quadCount}",
                $"Extraction took {stopwatch.Elapsed}"
            };
            File.WriteAllLines(StatsFileName, stats);

        }


        private List<string> ReadStatusFromJson()
        {
            if (!File.Exists(BuildStatusFileName))
                return new List<string>();

            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(BuildStatusFileName));
        }

        private void SaveStatusAsJson()
        {
            var processedJson = JsonConvert.SerializeObject(_processedCells);
            File.WriteAllText(BuildStatusFileName, processedJson);
        }
    }
}