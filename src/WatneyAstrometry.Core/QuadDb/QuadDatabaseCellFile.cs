// Copyright (c) Jussi Saarivirta.
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
        public QuadDatabaseCellFileDescriptor Descriptor { get; private set; }
        private static readonly int QuadDataLen = /*ratios*/ 6 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

        private ConcurrentQueue<FileStream> _fileStreamPool = new ConcurrentQueue<FileStream>();

        private readonly bool _bytesNeedReversing = false;

        public int FileId => _fileId;
        private readonly int _fileId;
        
        public QuadDatabaseCellFile(QuadDatabaseCellFileDescriptor descriptor, int fileId)
        {
            Descriptor = descriptor;
            _fileId = fileId;
            _bytesNeedReversing = descriptor.BytesNeedReversing;
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
        public unsafe StarQuad[] GetQuads(EquatorialCoords center, double angularDistance, int passIndex,
            int numSubSets, int subSetIndex, ImageStarQuad[] imageQuads, QuadDatabaseSolveInstanceMemoryCache cache)
        {
            // Quads that get a match, and are within search distance
            var matchingQuadsWithinRange = new List<StarQuad>();

            // Quads that get a match
            var matchingQuads = new List<StarQuad>();
            

            var pass = Descriptor.Passes[passIndex];
            var subCellsInRangeArr = new QuadDatabaseCellFileDescriptor.SubCellInfo[pass.SubCells.Length];
            var subCellsInRangeIndexesArr = new int[pass.SubCells.Length];
            var subCellsInRangeLen = 0;

            for (var p = 0; p < pass.SubCells.Length; p++)
            {
                var subCell = pass.SubCells[p];
                if (subCell.Center.GetAngularDistanceTo(center) - pass.AvgSubCellRadius < angularDistance)
                {
                    subCellsInRangeArr[subCellsInRangeLen] = subCell;
                    subCellsInRangeIndexesArr[subCellsInRangeLen] = p;
                    subCellsInRangeLen++;
                    //subCellsInRange.Add(subCell);
                }
            }

            var thisFileCache = cache.Files[_fileId];
            FileStream fileStream = null;

            // Pre-allocate, so that we don't need to allocate later down the road.
            var quadDataArray = new float[8];

            for (var sc = 0; sc < subCellsInRangeLen; sc++)
            {
                var subCellIdx = subCellsInRangeIndexesArr[sc];
                
                // Need to identify non-sampling cases, and maintain a separate cache for them;
                // When sampling is used, we use a number of subsets (== sampling parameter value) and we
                // cache the matching quads per subset. But when the final matching is done, we need to
                // use all possible quads - but not all are cached yet, so we can't just grab them from
                // all subsets and be done with it. Instead, maintain a separate cache for the non-sampled
                // matching runs. It's just easier that way.
                // Means we build and use the separate cache for non-sampled runs but that's fine.
                bool samplingBeingUsed = numSubSets > 1;
                StarQuad[] cachedQuads = null;

                if (!samplingBeingUsed)
                {
                    cachedQuads = thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsFullSet;
                }
                else if(thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset != null)
                {
                    cachedQuads = thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset[subSetIndex];
                }
                
                if (cachedQuads != null)
                {
                    foreach (var quad in cachedQuads)
                    {
                        if (quad != null && quad.MidPoint.GetAngularDistanceTo(center) < angularDistance)
                            matchingQuadsWithinRange.Add(quad);
                    }
                }
                else
                {
                    if (fileStream == null)
                    {
                        if (!_fileStreamPool.TryDequeue(out fileStream))
                            fileStream = new FileStream(Descriptor.Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }

                    if (samplingBeingUsed && thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset == null)
                    {
                        // Should cause no concern with multi-threading, since we're not processing multiple subsets in parallel.
                        thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset = new StarQuad[numSubSets][];
                    }
                    

                    byte[] subCellDataBytes = new byte[subCellsInRangeArr[sc].DataLengthBytes];
                    fileStream.Seek(subCellsInRangeArr[sc].DataStartPos, SeekOrigin.Begin);
                    fileStream.Read(subCellDataBytes, 0, subCellDataBytes.Length);
                    
                    fixed (byte* pSubCellDataBytes = subCellDataBytes)
                    {
                        int advance = 0;
                        var quadCount = subCellsInRangeArr[sc].DataLengthBytes / QuadDataLen;
                        // We will split the quadCount to numSubSets, and pick the quads in our assigned (sampling) subset.
                        var quadCountPerSubSet = quadCount / numSubSets;
                        var startIndex = quadCountPerSubSet * subSetIndex;
                        var nextStartIndex = subSetIndex == numSubSets - 1
                            ? quadCount
                            : startIndex + quadCountPerSubSet;
                        

                        advance = startIndex * QuadDataLen;
                        for (var q = startIndex; q < nextStartIndex; q++)
                        {
                            var quad = BytesToQuadNew(pSubCellDataBytes, advance, imageQuads, _bytesNeedReversing, quadDataArray);
                            
                            if (quad != null)
                                matchingQuads.Add(quad);
                            if (quad != null && quad.MidPoint.GetAngularDistanceTo(center) < angularDistance)
                                matchingQuadsWithinRange.Add(quad);

                            advance += QuadDataLen;
                        }
                    }

                    if (samplingBeingUsed)
                    {
                        thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset[subSetIndex] =
                            matchingQuads.ToArray();
                    }
                    else
                    {
                        thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsFullSet = 
                            matchingQuads.ToArray();
                    }

                    
                }
            }

            if(fileStream != null)
                _fileStreamPool.Enqueue(fileStream);

            return matchingQuadsWithinRange.ToArray();

            
        }

        private const float OnePer1023 = 0.0009775171065493f; // (1 / 1023)
        private const float OnePer511 = 0.001956947162f; // (1 / 511)

        /// <summary>
        /// Read the bytes and spit out a quad.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="tentativeMatches">If given, we only return the constructed quad if the ratios match to one of the tentativeMatches image quads.
        /// Otherwise do no matching and just read the quad from the buffer and return it.</param>
        /// <param name="bytesNeedReversing">Flag that indicates if we need to reverse byte order because of endianness difference between DB file contents and the system</param>
        /// <returns></returns>
        private static unsafe StarQuad BytesToQuadNew(byte* pBuf, int offset, ImageStarQuad[] tentativeMatches, bool bytesNeedReversing, float[] quadDataArray)
        {

            bool noMatching = tentativeMatches == null;
            byte* pRatios = (byte*)(pBuf + offset);
            float* pFloats = (float*)(pBuf + offset + 6); // floats come after the ratios
            
            // Ratios are packed; 3x 10 bit numbers, 2x 9 bit numbers.
            quadDataArray[0] = (((pRatios[1] << 8) & 0x3FF) + ((pRatios[0]) & 0x3FF)) * OnePer1023;
            quadDataArray[1] = ((((pRatios[2] & 0x0F) << 6) & 0x3FF) + ((pRatios[1] >> 2) & 0x3FF)) * OnePer1023;
            quadDataArray[2] = ((((pRatios[3] & 0x3F) << 4) & 0x3FF) + ((pRatios[2] >> 4) & 0x3FF)) * OnePer1023;
            quadDataArray[3] = ((((pRatios[4] & 0x7F) << 2) & 0x1FF) + ((pRatios[3] >> 6) & 0x1FF)) * OnePer511;
            quadDataArray[4] = ((((pRatios[5]) << 1) & 0x1FF) + ((pRatios[4] >> 7) & 0x1FF)) * OnePer511;


            if (noMatching)
            {
                // Ratios are always written in little endian and since we read byte by byte, endianness doesn't matter here.

                // Floats were written in whichever endianness the database was written in.
                if (bytesNeedReversing)
                {
                    byte* pFloatBytes = (byte*)(pBuf + offset + 6);
                    // largestDist
                    quadDataArray[5] = BitConverter.ToSingle(
                    new byte[] { pFloatBytes[3], pFloatBytes[2], pFloatBytes[1], pFloatBytes[0] }, 0);
                    // ra
                    quadDataArray[6] = BitConverter.ToSingle(
                        new byte[] { pFloatBytes[7], pFloatBytes[6], pFloatBytes[5], pFloatBytes[4] }, 0);
                    // dec
                    quadDataArray[7] = BitConverter.ToSingle(
                        new byte[] { pFloatBytes[11], pFloatBytes[10], pFloatBytes[9], pFloatBytes[8] }, 0);
                    
                }
                else
                {
                    quadDataArray[5] = pFloats[0]; // largestDist
                    quadDataArray[6] = pFloats[1]; // ra
                    quadDataArray[7] = pFloats[2]; // dec
                }

                // Need to allocate a new instance so that all quads don't refer to the same pre-allocated array instance...
                var ratios = new float[]
                {
                    quadDataArray[0],
                    quadDataArray[1],
                    quadDataArray[2],
                    quadDataArray[3],
                    quadDataArray[4]
                };
                var quad = new StarQuad(ratios, quadDataArray[5], new EquatorialCoords(quadDataArray[6], quadDataArray[7]));
                return quad;
            }

            for (var q = 0; q < tentativeMatches.Length; q++)
            {
                
                var imgQuad = tentativeMatches[q];
                
                if (imgQuad != null
                    && Math.Abs(imgQuad.Ratios[0] / quadDataArray[0] - 1.0f) <= 0.011f
                    && Math.Abs(imgQuad.Ratios[1] / quadDataArray[1] - 1.0f) <= 0.011f
                    && Math.Abs(imgQuad.Ratios[2] / quadDataArray[2] - 1.0f) <= 0.011f
                    && Math.Abs(imgQuad.Ratios[3] / quadDataArray[3] - 1.0f) <= 0.011f
                    && Math.Abs(imgQuad.Ratios[4] / quadDataArray[4] - 1.0f) <= 0.011f
                   )
                {
                    if (bytesNeedReversing)
                    {
                        byte* pFloatBytes = (byte*)(pBuf + offset + 5);
                        quadDataArray[5] = BitConverter.ToSingle(
                            new byte[] { pFloatBytes[3], pFloatBytes[2], pFloatBytes[1], pFloatBytes[0] }, 0);
                        quadDataArray[6] = BitConverter.ToSingle(
                            new byte[] { pFloatBytes[7], pFloatBytes[6], pFloatBytes[5], pFloatBytes[4] }, 0);
                        quadDataArray[7] = BitConverter.ToSingle(
                            new byte[] { pFloatBytes[11], pFloatBytes[10], pFloatBytes[9], pFloatBytes[8] }, 0);
                    }
                    else
                    {
                        quadDataArray[5] = pFloats[0]; // largestDist
                        quadDataArray[6] = pFloats[1]; // ra
                        quadDataArray[7] = pFloats[2]; // dec
                    }

                    // Need to allocate a new instance so that all quads don't refer to the same pre-allocated array instance...
                    var ratios = new float[]
                    {
                        quadDataArray[0], 
                        quadDataArray[1], 
                        quadDataArray[2], 
                        quadDataArray[3], 
                        quadDataArray[4]
                    };
                    var quad = new StarQuad(ratios, quadDataArray[5], new EquatorialCoords(quadDataArray[6], quadDataArray[7]));
                    return quad;
                }

            }

            return null;

              
            
        }

        public void Dispose()
        {
            while(_fileStreamPool.TryDequeue(out var fileStream))
                fileStream?.Dispose();
        }
    }
}