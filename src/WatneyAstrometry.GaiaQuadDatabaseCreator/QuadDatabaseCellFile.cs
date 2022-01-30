// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator
{
    public class QuadDatabaseCellFile : IDisposable
    {
        private readonly string _prefix = "gaia2";
        
        private List<Star> _allStars = new List<Star>();
        private readonly List<Star>[][] _subCellStars;
        public int QuadCount { get; private set; } = 0;
        public string StarSourceFile { get; private set; }

        private const string FileIdentifier = "WATNEYQDB";
        public const int FormatVersion = 2;

        public class Star
        {
            // The accuracy using floats should be enough, and saves space immensely.
            public float Ra;
            public float Dec;
            // Magnitude accuracy with byte is one decimal, which is enough.
            public byte Mag;
            public bool IsSelected = false;
            public EquatorialCoords RaDec => new EquatorialCoords(Ra, Dec);

            public class MagComparer : IComparer<Star>
            {
                public int Compare(Star x, Star y)
                {
                    return x.Mag < y.Mag ? -1 : x.Mag == y.Mag ? 0 : 1;
                }
            }

        }

        public class Quad
        {
            public ushort[] Ratios;
            public float LargestDistance;
            public EquatorialCoords CenterPoint;
            public Star[] Stars;
            
            public byte[] GetBytes() =>
                BitConverter.GetBytes(Ratios[0])
                    .Concat(BitConverter.GetBytes(Ratios[1]))
                    .Concat(BitConverter.GetBytes(Ratios[2]))
                    .Concat(BitConverter.GetBytes(Ratios[3]))
                    .Concat(BitConverter.GetBytes(Ratios[4]))
                    .Concat(BitConverter.GetBytes(LargestDistance))
                    .Concat(BitConverter.GetBytes((float)CenterPoint.Ra))
                    .Concat(BitConverter.GetBytes((float)CenterPoint.Dec))
                    .ToArray();

            //public static int Size => /*ratios*/ sizeof(float) * 5 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;
            public static int Size => /*ratios*/ sizeof(ushort) * 5 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

            /// <summary>
            /// For duplicate detection.
            /// </summary>
            internal class QuadEqualityComparer : IEqualityComparer<Quad>
            {
                public bool Equals(Quad x, Quad y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Stars.All(s => y.Stars.Contains(s));
                }

                public int GetHashCode(Quad obj)
                {
                    return obj.Stars[0].GetHashCode() ^ obj.Stars[1].GetHashCode() ^ obj.Stars[2].GetHashCode() ^
                           obj.Stars[3].GetHashCode();
                }
            }
        }

        public Cell CellReference { get; }
        
        //private readonly RaDecBounds[] _subCellRaDecBounds;
        private readonly int _starsPerSqDegree;
        private int _numPasses;
        private float _passFactor;
        private int[] _passSubDivisions;
        

        private bool _disposing = false;
        private readonly int _startPassIndex;
        private readonly int _endPassIndex;

        private RaDecBounds[][] _subDivisionBounds;

        public QuadDatabaseCellFile(Cell cell, int starsPerSquareDeg, float passFactor, int startPassIndex, int endPassIndex, string starSourceFile)
        {
            CellReference = cell;
            StarSourceFile = starSourceFile;
            _starsPerSqDegree = starsPerSquareDeg;
            _startPassIndex = startPassIndex;
            _endPassIndex = endPassIndex;
            _passFactor = passFactor;
            _numPasses = endPassIndex - startPassIndex + 1;
            _passSubDivisions = new int[_numPasses];
            _subDivisionBounds = new RaDecBounds[_numPasses][];
            _subCellStars = new List<Star>[_numPasses][];
            

            for (var i = 0; i < _numPasses; i++)
            {
                var subDivisionsInPass = GetSubdivisions(_startPassIndex + i);
                _passSubDivisions[i] = subDivisionsInPass;
                _subDivisionBounds[i] = CellReference.SubDivide(subDivisionsInPass);
                _subCellStars[i] = new List<Star>[_subDivisionBounds[i].Length];
                for (var j = 0; j < _subCellStars[i].Length; j++)
                    _subCellStars[i][j] = new List<Star>();
                // _subCellStars[pass][subCell]
            }
            
        }

        private int GetSubdivisions(int pass)
        {
            var approxCellSizeSqDeg = CellReference.HeightDeg * CellReference.WidthDeg;
            var starsInCell = _starsPerSqDegree * Math.Pow(_passFactor, pass) * approxCellSizeSqDeg;
            var splitCount = 2000;

            return (int)Math.Min(Math.Max(2.0, starsInCell / splitCount), 12.0);
        }


        public void AddStar(Star star)
        {
            _allStars.Add(star);
            
            for (var pass = 0; pass < _numPasses; pass++)
            {
                var subDivBounds = _subDivisionBounds[pass];

                for (var subCellIdx = 0; subCellIdx < subDivBounds.Length; subCellIdx++)
                {
                    var subCellRaDecBounds = _subDivisionBounds[pass][subCellIdx];
                    if (subCellRaDecBounds.IsInside(star.RaDec))
                    {
                        _subCellStars[pass][subCellIdx].Add(star);
                        break;
                    }
                }
                
            }
            
        }


        internal static List<Quad> FormQuads(IList<Star> stars)
        {
            var quads = new List<Quad>();
            
            // Avoided using Linq here, as we run this method quite a few times.
            // Literally halved the time spent here by removing Linq and Dictionary usage.

            var starDistances = new double[stars.Count][];
            for (var i = 0; i < starDistances.Length; i++)
                starDistances[i] = new double[starDistances.Length];

            for (var i = 0; i < stars.Count; i++)
            {
                starDistances[i][i] = 0;
                for (var j = i + 1; j < stars.Count; j++)
                {
                    var dist = EquatorialCoords.GetAngularDistanceBetween(
                        new EquatorialCoords(stars[i].Ra, stars[i].Dec), new EquatorialCoords(stars[j].Ra, stars[j].Dec));
                    starDistances[i][j] = dist;
                    starDistances[j][i] = dist;
                }
            }

            for (var i = 0; i < starDistances.Length; i++)
            {
                var starIndex0 = i;
                var distancesToOthers = starDistances[starIndex0];

                // Get 3 nearest
                var nearestIndices = new int[3] { -1, -1, -1 };
                var nearestDistances = new double[3];

                for (var n = 0; n < 3; n++)
                {
                    int index = 0;
                    var dist = double.MaxValue;
                    for (var j = 0; j < distancesToOthers.Length; j++)
                    {
                        if (distancesToOthers[j] < dist && distancesToOthers[j] > 0 && j != nearestIndices[0] && j != nearestIndices[1])
                        {
                            dist = distancesToOthers[j];
                            index = j;
                        }
                    }

                    nearestIndices[n] = index;
                    nearestDistances[n] = dist;
                }


                var d0a = nearestDistances[0];
                var d0b = nearestDistances[1];
                var d0c = nearestDistances[2];

                var starIndexA = nearestIndices[0];
                var starIndexB = nearestIndices[1];
                var starIndexC = nearestIndices[2];

                var dab = starDistances[starIndexA][starIndexB];
                var dac = starDistances[starIndexA][starIndexC];
                var dbc = starDistances[starIndexB][starIndexC];

                var sixDistances = new List<double> { d0a, d0b, d0c, dab, dac, dbc };
                sixDistances.Sort();
                var largestDistance = sixDistances.Max();

                //// Too big for our set, abandon.
                //if (largestDistance > maxQuadSize)
                //    continue;

                sixDistances.RemoveAt(sixDistances.IndexOf(largestDistance));
                var ratios = sixDistances
                    .Select(x => (float)(x / largestDistance))
                    .ToArray();

                var quadStars = new Star[]
                {
                    stars[starIndex0],
                    stars[starIndexA],
                    stars[starIndexB],
                    stars[starIndexC]
                };
                var centerPoint = EquatorialCoords.GetCenterEquatorialCoords(new[]
                {
                    stars[starIndex0].RaDec, 
                    stars[starIndexA].RaDec, 
                    stars[starIndexB].RaDec, 
                    stars[starIndexC].RaDec
                });

                var shortRatios = new ushort[5];
                shortRatios[0] = (ushort) (ratios[0] * 100000 / 2.0);
                shortRatios[1] = (ushort) (ratios[1] * 100000 / 2.0);
                shortRatios[2] = (ushort) (ratios[2] * 100000 / 2.0);
                shortRatios[3] = (ushort) (ratios[3] * 100000 / 2.0);
                shortRatios[4] = (ushort) (ratios[4] * 100000 / 2.0);

                var quad = new Quad
                {
                    CenterPoint = centerPoint,
                    LargestDistance = (float) largestDistance,
                    Ratios = shortRatios,
                    Stars = quadStars
                };
                //var quad = (ratios, largestDistance, quadStars, centerPoint);
                quads.Add(quad);
            }

            quads = quads.Distinct(new Quad.QuadEqualityComparer()).ToList();
            return quads;
        }
        

        private class PassWithSubCells
        {
            public List<Quad>[] SubCellQuads { get; set; }

            public int QuadCount =>
                SubCellQuads.Sum(sc => sc.Count);

            public int SubCellCount => SubCellQuads.Length;

            public PassWithSubCells(int subCellCount)
            {
                SubCellQuads = new List<Quad>[subCellCount];
                for (var i = 0; i < subCellCount; i++)
                    SubCellQuads[i] = new List<Quad>();
            }
            
        }

        private void WriteHeaderData(Stream stream, List<PassWithSubCells> passesWithSubCells)
        {
            var c = CultureInfo.InvariantCulture;
            
            // This header is just for show; it's human readable, and can show the file content at a glance.
            // The machine reads the binary data instead.
            var header = new
            {
                FileFormatVersion = $"{FormatVersion}",
                Description = "Star quads formed from Gaia2 catalog stars",
                CellId = CellReference.CellId,
                StartPass = _startPassIndex,
                EndPass = _endPassIndex,
                PassFactor = _passFactor,
                StarsPerSqDeg = _starsPerSqDegree,
                SourceDataSet = "Gaia2",
                RaBounds = $"[{CellReference.Bounds.RaLeft.ToString(c)}, {CellReference.Bounds.RaRight.ToString(c)}]",
                DecBounds = $"[{CellReference.Bounds.DecBottom.ToString(c)}, {CellReference.Bounds.DecTop.ToString(c)}]"
            };

            // Write file identifier.
            stream.Write(Encoding.ASCII.GetBytes(FileIdentifier));

            // Write version number.
            stream.Write(BitConverter.GetBytes(FormatVersion));

            // Write header information.
            stream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(header, Formatting.Indented)));

            // Write null character; data begins after this.
            stream.WriteByte(0);

            // Write band and cell index.
            stream.Write(BitConverter.GetBytes(CellReference.BandIndex));
            stream.Write(BitConverter.GetBytes(CellReference.CellIndex));

            // The number of passes.
            stream.Write(BitConverter.GetBytes(_numPasses));

            var densities = new float[_numPasses];

            // For each pass, write key header information: density, SubCell center and the length of the data block.
            for (var pass = 0; pass < _numPasses; pass++)
            {
                var numberOfQuads = passesWithSubCells[pass].QuadCount;
                var approxCellSizeSqDeg = CellReference.HeightDeg * CellReference.WidthDeg;
                float approxQuadsPerSqDeg = (float)(numberOfQuads / approxCellSizeSqDeg);
                densities[pass] = approxQuadsPerSqDeg;

                // Approximate quad density in this pass, quads per degree.
                stream.Write(BitConverter.GetBytes(approxQuadsPerSqDeg));

                // How many subdivisions (number of SubCells should be ^2)
                stream.Write(BitConverter.GetBytes(_passSubDivisions[pass]));

                // Number of SubCells.
                stream.Write(BitConverter.GetBytes(passesWithSubCells[pass].SubCellCount));

                // For each SubCell: store SubCell center RA, Dec and the data block size in bytes.
                for (var subCellIndex = 0; subCellIndex < passesWithSubCells[pass].SubCellCount; subCellIndex++)
                {
                    var centerCoords = _subDivisionBounds[pass][subCellIndex];
                    var quadDataBlockSize = passesWithSubCells[pass].SubCellQuads[subCellIndex].Count * Quad.Size;

                    float centerRa = (float)centerCoords.Center.Ra;
                    float centerDec = (float)centerCoords.Center.Dec;

                    stream.Write(BitConverter.GetBytes(centerRa));
                    stream.Write(BitConverter.GetBytes(centerDec));
                    stream.Write(BitConverter.GetBytes(quadDataBlockSize));
                }
            }

            Console.WriteLine($"{CellReference.CellId}: Pass quad densities [{string.Join(", ", densities.Select(d => d.ToString("F1", CultureInfo.InvariantCulture)))}]");
        }

        private void WriteQuadData(Stream stream, List<PassWithSubCells> passesWithSubCells)
        {
            for (var pass = 0; pass < _numPasses; pass++)
            {
                for (var subCellIndex = 0; subCellIndex < passesWithSubCells[pass].SubCellCount; subCellIndex++)
                {
                    var subCellQuads = passesWithSubCells[pass].SubCellQuads[subCellIndex];
                    for (var quad = 0; quad < subCellQuads.Count; quad++)
                    {
                        stream.Write(subCellQuads[quad].GetBytes());
                    }
                }
            }
        }

        public void Serialize(string outputDir)
        {

            Console.WriteLine($"{CellReference.CellId}: Constructing and serializing...");
            
            var approxCellSizeSqDeg = CellReference.HeightDeg * CellReference.WidthDeg;
            _allStars.Sort(new Star.MagComparer());

            var passesWithSubCells = new List<PassWithSubCells>();
            for (var pass = 0; pass < _numPasses; pass++)
            {
                var numSubCells = _subDivisionBounds[pass].Length;
                passesWithSubCells.Add(new PassWithSubCells(numSubCells));
            }

            for (var pass = 0; pass < _numPasses; pass++)
            {
                var starsInPass = (int)(_starsPerSqDegree * Math.Pow(_passFactor, (_startPassIndex + pass)) * approxCellSizeSqDeg);
                var numSubCells = _subDivisionBounds[pass].Length;

                for(var i = 0; i < starsInPass; i++)
                    if (i < _allStars.Count)
                        _allStars[i].IsSelected = true;
                
                // In parallel, form quads per each SubCell.
                Parallel.For(0, numSubCells, (subCellIdx, state) =>
                {
                    var starList = _subCellStars[pass][subCellIdx];
                    var subCellSelectedStars = starList.Where(s => s.IsSelected).ToArray();
                    if (subCellSelectedStars.Length > 4)
                        passesWithSubCells[pass].SubCellQuads[subCellIdx].AddRange(FormQuads(subCellSelectedStars));
                });

                for (var i = 0; i < starsInPass; i++)
                    if (i < _allStars.Count)
                        _allStars[i].IsSelected = false;
            }

            var filename = Path.Combine(outputDir, $"{_prefix}-{CellReference.CellId}-{_startPassIndex:00}-{_endPassIndex:00}-{_starsPerSqDegree}.qdb");

            Console.WriteLine($"{CellReference.CellId}: Writing output");
            using (var stream = new FileStream(filename, FileMode.Create))
            {
                WriteHeaderData(stream, passesWithSubCells);
                WriteQuadData(stream, passesWithSubCells);
            }

            QuadCount = passesWithSubCells.Sum(p => p.QuadCount);


        }


        public void Dispose()
        {
            if (!_disposing)
            {
                _disposing = true;
                _allStars.Clear();
                _allStars = null;
                foreach(var x in _subCellStars)
                    foreach(var n in x)
                        n.Clear();
            }
        }
    }
}