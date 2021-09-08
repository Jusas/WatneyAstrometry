﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    /// <summary>
    /// A class that represents a single Cell file (a file that contains quads in
    /// passes for specified RA,Dec bounds, a part of the quad database).
    /// </summary>
    public class QuadDatabaseCellFile : IDisposable
    {
        public const string FileIdentifier = "WATNEYQDB";
        private static readonly int QuadDataLen = /*ratios*/ sizeof(ushort) * 5 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

        private ConcurrentQueue<FileStream> _fileStreamPool = new ConcurrentQueue<FileStream>();

        public class SubCellInfo
        {
            public EquatorialCoords Center { get; set; }
            public int DataLengthBytes { get; set; }
            public long DataStartPos { get; set; }
        }

        public class Pass
        {
            public float QuadsPerSqDeg { get; set; }
            public int SubDivisions { get; set; }
            public SubCellInfo[] SubCells { get; set; }
            public double AvgSubCellRadius { get; set; }
        }

        public class Descriptor
        {
            public string CellId { get; set; }
            public Pass[] Passes { get; set; }
        }

        public string Filename { get; private set; }
        public Descriptor FileDescriptor { get; private set; }

        public QuadDatabaseCellFile(string filename)
        {
            Filename = filename;
            FileDescriptor = ReadDescriptor(filename);
        }

        /// <summary>
        /// Read the 'header' of the file.
        /// The header contains info about the passes and sub cells contained.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Descriptor ReadDescriptor(string filename)
        {
            var descriptor = new Descriptor();
            long dataStartPos = 0;
            Cell cellReference;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {

                var fileIdBytes = Encoding.ASCII.GetBytes(FileIdentifier);

                if (stream.Length < fileIdBytes.Length)
                    throw new Exception("Invalid file format");

                var idBuf = reader.ReadBytes(fileIdBytes.Length);
                var idString = Encoding.ASCII.GetString(idBuf);
                if (idString != FileIdentifier)
                    throw new Exception($"Invalid file format: expected {FileIdentifier}");

                // File format, check that this format is supported.
                var fileFormatVersion = reader.ReadInt32();
                if (fileFormatVersion != 2)
                    throw new Exception("This file format version is not supported");
                
                // File starts with human readable header, skip it.
                while (reader.ReadByte() != 0) ;

                // Band and cell indices
                var band = reader.ReadInt32();
                var cell = reader.ReadInt32();
                descriptor.CellId = Cell.GetCellId(band, cell);
                cellReference = SkySegmentSphere.GetCellById(descriptor.CellId);

                // Number of passes
                int passCount = reader.ReadInt32();
                descriptor.Passes = new Pass[passCount];

                // For each pass, read: quadsPerSqDeg, number of SubCells
                for (var p = 0; p < passCount; p++)
                {
                    var pass = new Pass();
                    descriptor.Passes[p] = pass;
                    pass.QuadsPerSqDeg = reader.ReadSingle();
                    pass.SubDivisions = reader.ReadInt32();
                    
                    var numSubCells = reader.ReadInt32();
                    pass.SubCells = new SubCellInfo[numSubCells];

                    for (var sc = 0; sc < numSubCells; sc++)
                    {
                        var ra = reader.ReadSingle();
                        var dec = reader.ReadSingle();
                        var dataLength = reader.ReadInt32();

                        var subCell = new SubCellInfo()
                        {
                            Center = new EquatorialCoords(ra, dec),
                            DataLengthBytes = dataLength
                        };
                        pass.SubCells[sc] = subCell;
                    }
                }

                dataStartPos = stream.Position;
            }

            for (var p = 0; p < descriptor.Passes.Length; p++)
            {
                var pass = descriptor.Passes[p];
                for (var s = 0; s < pass.SubCells.Length; s++)
                {
                    pass.SubCells[s].DataStartPos = dataStartPos;
                    dataStartPos += pass.SubCells[s].DataLengthBytes;
                    
                    if (pass.SubCells.Length == 1)
                        pass.AvgSubCellRadius = 0.5 * EquatorialCoords.GetAngularDistanceBetween(
                            new EquatorialCoords(cellReference.Bounds.RaLeft, cellReference.Bounds.DecTop),
                            new EquatorialCoords(cellReference.Bounds.RaRight, cellReference.Bounds.DecBottom));
                    else
                    {
                        var spanningDistance = EquatorialCoords.GetAngularDistanceBetween(
                            pass.SubCells.First().Center,
                            pass.SubCells.Last().Center);
                        pass.AvgSubCellRadius = spanningDistance / (pass.SubDivisions - 1) / 2;
                    }
                }

                
            }

            return descriptor;
        }

        /// <summary>
        /// Get the quads within range that are potential matches for the image quads.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="angularDistance"></param>
        /// <param name="passIndex"></param>
        /// <param name="numSubSets">The number of quad subsets we divide the available quads to.</param>
        /// <param name="subSetIndex">The index of the quad subset we want to include.</param>
        /// <param name="imageQuads"></param>
        /// <returns></returns>
        public StarQuad[] GetQuads(EquatorialCoords center, double angularDistance, int passIndex, int numSubSets, int subSetIndex, ImageStarQuad[] imageQuads)
        {
            var foundQuads = new List<StarQuad>();
            var subCellsInRange = new List<SubCellInfo>();

            var pass = FileDescriptor.Passes[passIndex];

            // TODO: the higher the subset count, the more this gets called and it does add up.
            for (var p = 0; p < pass.SubCells.Length; p++)
            {
                var subCell = pass.SubCells[p];
                if (subCell.Center.GetAngularDistanceTo(center) - pass.AvgSubCellRadius < angularDistance)
                {
                    subCellsInRange.Add(subCell);
                }
            }

            var subCellsInRangeArr = subCellsInRange.ToArray();

            // Grab a copy; we will use this to reduce the amount of matching checks we need to make,
            // by setting non-matching items to null. Not using a list, because initializing lists is
            // expensive.
            var imageQuadsCopy = imageQuads?.ToArray();

            FileStream fileStream;
            if (!_fileStreamPool.TryDequeue(out fileStream))
                fileStream = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.Read);

            // TODO: filestream reads and seeks are taking the toll when using large subset count. Could potentially speed things up with caching at the cost of memory usage.
            for (var sc = 0; sc < subCellsInRangeArr.Length; sc++)
            {
                fileStream.Seek(subCellsInRangeArr[sc].DataStartPos, SeekOrigin.Begin);
                var dataBuf = new byte[subCellsInRangeArr[sc].DataLengthBytes];
                fileStream.Read(dataBuf, 0, dataBuf.Length);

                int advance = 0;
                var quadCount = dataBuf.Length / QuadDataLen;
                // We will split the quadCount to numSubSets, and pick the quads in our assigned subset.
                var quadCountPerSubSet = quadCount / numSubSets;
                var startIndex = quadCountPerSubSet * subSetIndex;
                var nextStartIndex = subSetIndex == numSubSets - 1
                    ? quadCount
                    : startIndex + quadCountPerSubSet;

                advance = startIndex * QuadDataLen;
                for (var q = startIndex; q < nextStartIndex; q++)
                {
                    var quad = BytesToQuadNew(dataBuf, advance, imageQuadsCopy);
                    if (quad != null && quad.MidPoint.GetAngularDistanceTo(center) < angularDistance)
                        foundQuads.Add(quad);
                    advance += QuadDataLen;
                    // Reset the array for next loop round.
                    if (imageQuadsCopy == null) continue;
                    //for (var iq = 0; iq < imageQuadsCopy.Length; iq++)
                    //    imageQuadsCopy[iq] = imageQuads[iq];
                }
                
            }

            _fileStreamPool.Enqueue(fileStream);
            

            return foundQuads.ToArray();
            
        }
        

        /// <summary>
        /// Read the bytes and spit out a quad.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="tentativeMatches"></param>
        /// <returns></returns>
        private static unsafe StarQuad BytesToQuadNew(byte[] buf, int offset, ImageStarQuad[] tentativeMatches)
        {
            // Optimized: we try to detect unfit quads as early as possible, and we try to
            // do as little work as possible in order to achieve that. We can shave off some seconds
            // from blind solves by doing this.

            bool noMatching = tentativeMatches == null;
            const float divider = 50_000.0f;
            fixed (byte* pBuf = buf)
            {
                
                ushort* pRatios = (ushort*)(pBuf + offset);

                if (noMatching)
                {
                    var ratios = new[] {pRatios[0] / divider, pRatios[1] / divider, pRatios[2] / divider, pRatios[3] / divider, pRatios[4] / divider};

                    float* pFloats = (float*)(pBuf + offset + sizeof(ushort) * 5);
                    var largestDist = pFloats[0];
                    var ra = pFloats[1];
                    var dec = pFloats[2];

                    var quad = new StarQuad(ratios, largestDist, new EquatorialCoords(ra, dec));
                    return quad;
                }

                for (var q = 0; q < tentativeMatches.Length; q++)
                {
                    var imgQuad = tentativeMatches[q];
                    if (imgQuad != null
                        && Math.Abs(imgQuad.Ratios[0] / (pRatios[0] / divider) - 1.0f) <= 0.01f
                        && Math.Abs(imgQuad.Ratios[1] / (pRatios[1] / divider) - 1.0f) <= 0.01f
                        && Math.Abs(imgQuad.Ratios[2] / (pRatios[2] / divider) - 1.0f) <= 0.01f
                        && Math.Abs(imgQuad.Ratios[3] / (pRatios[3] / divider) - 1.0f) <= 0.01f
                        && Math.Abs(imgQuad.Ratios[4] / (pRatios[4] / divider) - 1.0f) <= 0.01f
                    )
                    {
                        var ratios = new[] {pRatios[0] / divider, pRatios[1] / divider, pRatios[2] / divider, pRatios[3] / divider, pRatios[4] / divider};

                        float* pFloats = (float*)(pBuf + offset + sizeof(ushort) * 5);
                        var largestDist = pFloats[0];
                        var ra = pFloats[1];
                        var dec = pFloats[2];

                        var quad = new StarQuad(ratios, largestDist, new EquatorialCoords(ra, dec));
                        return quad;
                    }

                }

                return null;

                
            }
            
        }

        public void Dispose()
        {
            while(_fileStreamPool.TryDequeue(out var fileStream))
                fileStream?.Dispose();
        }
    }
}