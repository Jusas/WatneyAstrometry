// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WatneyAstrometry.Core.Exceptions;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    /// <summary>
    /// A class that represents a single Cell file (a file that contains quads in
    /// passes for specified RA,Dec bounds, a part of the quad database).
    /// </summary>
    internal class QuadDatabaseCellFile : IDisposable
    {
        public QuadDatabaseCellFileDescriptor Descriptor { get; private set; }
        private static readonly int QuadDataLen = /*ratios*/ 6 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

        private ConcurrentQueue<FileStream> _fileStreamPool = new ConcurrentQueue<FileStream>();

        private readonly bool _bytesNeedReversing = false;

        public int FileId => _fileId;
        private readonly int _fileId;

        private bool _fileVersionValidated = false;
        private const string FileFormatIdentifierString = "WATNEYQDB";
        private const int FileFormatVersion = 3;
        
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
        /// <param name="sortedImageQuads">Image star quads, sorted by the first ratio (descending).</param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public unsafe StarQuad[] GetQuads(EquatorialCoords center, double angularDistance, int passIndex,
            int numSubSets, int subSetIndex, ImageStarQuad[] sortedImageQuads, QuadDatabaseSolveInstanceMemoryCache cache)
        {
            // Quads that get a match, and are within search distance
            var matchingQuadsWithinRange = new List<StarQuad>();

            // Quads that get a match
            var matchingQuads = new List<StarQuad>();
            

            var pass = Descriptor.Passes[passIndex];
            var subCellsInRangeArr = new QuadDatabaseCellFileDescriptor.SubCellInfo[pass.SubCells.Length];
            var subCellsInRangeIndexesArr = new int[pass.SubCells.Length];
            var subCellsInRangeLen = 0;

            var decThreshold = angularDistance + pass.AvgSubCellRadius;
            for (var p = 0; p < pass.SubCells.Length; p++)
            {
                var subCell = pass.SubCells[p];
                // Fast Dec pre-filter: Dec difference is always <= great-circle distance,
                // so if it already exceeds the threshold the full check can't pass.
                if (Math.Abs(subCell.Center.Dec - center.Dec) >= decThreshold)
                    continue;
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
                        try
                        {
                            var streamOptions = new FileStreamOptions()
                            {
                                Options = FileOptions.RandomAccess, 
                                Access = FileAccess.Read, 
                                BufferSize = 0, 
                                Mode = FileMode.Open, 
                                Share = FileShare.Read
                            };
                            if (!_fileStreamPool.TryDequeue(out fileStream))
                            {
                                fileStream = new FileStream(Descriptor.Filename, streamOptions);
                                // fileStream = new FileStream(Descriptor.Filename, FileMode.Open, FileAccess.Read,
                                //     FileShare.Read, 4096, false); // Do not use async; testing in net10 if this improves performance.
                            }

                            if (!_fileVersionValidated)
                            {
                                _fileVersionValidated = true;
                                fileStream.Seek(0, SeekOrigin.Begin);
                                var fileFormatBytes = new byte[FileFormatIdentifierString.Length];
                                fileStream.ReadExactly(fileFormatBytes, 0, fileFormatBytes.Length);

                                // Note to self: why wasn't I smart enough to use byte for version number? It's not like there will be many, and the there's endianness...
                                if (Encoding.ASCII.GetString(fileFormatBytes, 0, FileFormatIdentifierString.Length) !=
                                    FileFormatIdentifierString)
                                {
                                    throw new QuadDatabaseVersionException($"The file {Descriptor.Filename} is not a valid Watney database file");
                                }
                                
                                var versionNumBytes = new byte[sizeof(int)];
                                fileStream.ReadExactly(versionNumBytes, 0, sizeof(int));
                                if (Descriptor.BytesNeedReversing)
                                    Array.Reverse(versionNumBytes);

                                var versionNum = BitConverter.ToInt32(versionNumBytes, 0);
                                if (versionNum != FileFormatVersion)
                                    throw new QuadDatabaseVersionException(
                                        $"Expected database version {FileFormatVersion} format database files, but they were version {versionNum}. " +
                                        $"Unable to use them. Make sure you have downloaded the right database files.");
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            throw new QuadDatabaseException(
                                $"Quad database file {Descriptor.Filename} was not found. Is your quad database intact?");
                        }
                        catch (Exception e)
                        {
                            throw new QuadDatabaseException($"Failed to read quad database file {Descriptor.Filename}: {e.Message}", e);
                        }
                        
                    }

                    if (samplingBeingUsed && thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset == null)
                    {
                        // Should cause no concern with multi-threading, since we're not processing multiple subsets in parallel.
                        thisFileCache.Passes[passIndex].SubCells[subCellIdx].QuadsForSubset = new StarQuad[numSubSets][];
                    }
                    
                    var quadCountInSubCell = subCellsInRangeArr[sc].DataLengthBytes / QuadDataLen;
                    var quadCountPerSubSet = quadCountInSubCell / numSubSets;
                    var quadSplitModulo = quadCountInSubCell % numSubSets;
                    
                    long streamReadOffset = subCellsInRangeArr[sc].DataStartPos +
                                        subSetIndex * quadCountPerSubSet * QuadDataLen;
                    
                    var numberOfQuadsToRead = subSetIndex == numSubSets - 1 && quadSplitModulo > 0
                        ? quadCountPerSubSet + quadSplitModulo // Add modulo quads to the last subset
                        : quadCountPerSubSet;
                    byte[] subSetDataBytes = new byte[numberOfQuadsToRead * QuadDataLen];
                    
                    fileStream.Seek(streamReadOffset, SeekOrigin.Begin);
                    fileStream.ReadExactly(subSetDataBytes, 0, subSetDataBytes.Length);
                    
                    fixed (byte* pSubSetDataBytes = subSetDataBytes)
                    {
                        int advance = 0;
                        for (var q = 0; q < numberOfQuadsToRead; q++)
                        {
                            var quad = BytesToQuadNew(pSubSetDataBytes, advance, sortedImageQuads, _bytesNeedReversing);
                            advance += QuadDataLen;
                            if (quad == null)
                                continue;
                            
                            matchingQuads.Add(quad);
                            if (quad.MidPoint.GetAngularDistanceTo(center) < angularDistance)
                                matchingQuadsWithinRange.Add(quad);
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
        private const float RatioMatchLow  = 1.0f - 0.011f; // 0.989
        private const float RatioMatchHigh = 1.0f + 0.011f; // 1.011

        /// <summary>
        /// Read the bytes and spit out a quad.
        /// </summary>
        /// <param name="pBuf"></param>
        /// <param name="offset"></param>
        /// <param name="sortedTentativeMatches">If given, we only return the constructed quad if the ratios match to one of the tentativeMatches image quads.
        /// Otherwise do no matching and just read the quad from the buffer and return it.
        /// This list is sorted by the first ratio (descending).</param>
        /// <param name="bytesNeedReversing">Flag that indicates if we need to reverse byte order because of endianness difference between DB file contents and the system</param>
        /// <returns></returns>
        private static unsafe StarQuad BytesToQuadNew(byte* pBuf, int offset, ImageStarQuad[] sortedTentativeMatches, bool bytesNeedReversing)
        {
            // TODO would it be simpler if we moved the byte reversing stuff to another method? How would it affect performance too?
            bool noMatching = sortedTentativeMatches == null;
            byte* pRatios = (byte*)(pBuf + offset);
            byte* pFloats = (byte*)(pBuf + offset + 6); // floats come after the ratios

            // Ratios are packed; 3x 10 bit numbers, 2x 9 bit numbers.
            float r0 = (((pRatios[1] << 8) & 0x3FF) + ((pRatios[0]) & 0x3FF)) * OnePer1023;
            float r1 = ((((pRatios[2] & 0x0F) << 6) & 0x3FF) + ((pRatios[1] >> 2) & 0x3FF)) * OnePer1023;
            float r2 = ((((pRatios[3] & 0x3F) << 4) & 0x3FF) + ((pRatios[2] >> 4) & 0x3FF)) * OnePer1023;
            float r3 = ((((pRatios[4] & 0x7F) << 2) & 0x1FF) + ((pRatios[3] >> 6) & 0x1FF)) * OnePer511;
            float r4 = ((((pRatios[5]) << 1) & 0x1FF) + ((pRatios[4] >> 7) & 0x1FF)) * OnePer511;

            if (noMatching)
            {
                // Ratios are always written in little endian and since we read byte by byte, endianness doesn't matter here.

                // Floats were written in whichever endianness the database was written in.
                float ld, ra, dec;
                if (bytesNeedReversing)
                {
                    ld  = BitConverter.ToSingle(new byte[] { pFloats[3],  pFloats[2],  pFloats[1],  pFloats[0]  }, 0);
                    ra  = BitConverter.ToSingle(new byte[] { pFloats[7],  pFloats[6],  pFloats[5],  pFloats[4]  }, 0);
                    dec = BitConverter.ToSingle(new byte[] { pFloats[11], pFloats[10], pFloats[9],  pFloats[8]  }, 0);
                }
                else
                {
                    // Note: On ARM, misaligned floats and doubles have to be read/written from memory as int/long and
                    // converted to/from float/double via local variable that is guaranteed to be aligned.
                    // https://github.com/dotnet/runtime/issues/18041
                    // So we have to do this, otherwise ARMv7 breaks with "A datatype misalignment was detected in a load or store instruction."
                    var if1 = *(int*)pFloats; pFloats += sizeof(int);
                    var if2 = *(int*)pFloats; pFloats += sizeof(int);
                    var if3 = *(int*)pFloats;
                    ld  = *(float*)&if1;
                    ra  = *(float*)&if2;
                    dec = *(float*)&if3;
                }

                return new StarQuad(new QuadRatios(r0, r1, r2, r3, r4), ld, new EquatorialCoords(ra, dec));
            }

            var ratios = new QuadRatios(r0, r1, r2, r3, r4);
            var lo = ratios * RatioMatchLow;
            var hi = ratios * RatioMatchHigh;

            for (var q = 0; q < sortedTentativeMatches.Length; q++)
            {
                var imgQuad = sortedTentativeMatches[q];
                if (   imgQuad.Ratios.R0 >= lo.R0 && imgQuad.Ratios.R0 <= hi.R0
                    && imgQuad.Ratios.R1 >= lo.R1 && imgQuad.Ratios.R1 <= hi.R1
                    && imgQuad.Ratios.R2 >= lo.R2 && imgQuad.Ratios.R2 <= hi.R2
                    && imgQuad.Ratios.R3 >= lo.R3 && imgQuad.Ratios.R3 <= hi.R3
                    && imgQuad.Ratios.R4 >= lo.R4 && imgQuad.Ratios.R4 <= hi.R4
                   )
                {
                    float ld, ra, dec;
                    if (bytesNeedReversing)
                    {
                        ld  = BitConverter.ToSingle(new byte[] { pFloats[3],  pFloats[2],  pFloats[1],  pFloats[0]  }, 0);
                        ra  = BitConverter.ToSingle(new byte[] { pFloats[7],  pFloats[6],  pFloats[5],  pFloats[4]  }, 0);
                        dec = BitConverter.ToSingle(new byte[] { pFloats[11], pFloats[10], pFloats[9],  pFloats[8]  }, 0);
                    }
                    else
                    {
                        // Note: On ARM, misaligned floats and doubles have to be read/written from memory as int/long and
                        // converted to/from float/double via local variable that is guaranteed to be aligned.
                        // https://github.com/dotnet/runtime/issues/18041
                        // So we have to do this, otherwise ARMv7 breaks with "A datatype misalignment was detected in a load or store instruction."
                        var if1 = *(int*)pFloats; pFloats += sizeof(int);
                        var if2 = *(int*)pFloats; pFloats += sizeof(int);
                        var if3 = *(int*)pFloats;
                        ld  = *(float*)&if1;
                        ra  = *(float*)&if2;
                        dec = *(float*)&if3;
                    }

                    return new StarQuad(ratios, ld, new EquatorialCoords(ra, dec));
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