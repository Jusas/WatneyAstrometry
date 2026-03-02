// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
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
        private const int QuadDataLen = /*ratios*/ 6 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

        private readonly bool _bytesNeedReversing = false;

        public int FileId => _fileId;
        private readonly int _fileId;

        private const string FileFormatIdentifierString = "WATNEYQDB";
        private const int FileFormatVersion = 3;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _mmvAccessor;
        // Pointer to byte 0 of the file data (adjusted for PointerOffset).
        // Stored as nint so the field doesn't require an unsafe context on the class.
        private nint _pFileStart;
        private bool _mmfInitialized = false;
        private readonly object _initLock = new object();

        public QuadDatabaseCellFile(QuadDatabaseCellFileDescriptor descriptor, int fileId)
        {
            Descriptor = descriptor;
            _fileId = fileId;
            _bytesNeedReversing = descriptor.BytesNeedReversing;
        }

        /// <summary>
        /// Opens and memory-maps the file on first use, validating the format header.
        /// Subsequent calls are a cheap boolean check.
        /// </summary>
        private unsafe void EnsureMmfOpen()
        {
            if (_mmfInitialized) return;

            lock (_initLock)
            {
                if (_mmfInitialized) return;

                MemoryMappedFile mmf = null;
                MemoryMappedViewAccessor accessor = null;
                bool pointerAcquired = false;
                byte* pBase = null;
                try
                {
                    mmf = MemoryMappedFile.CreateFromFile(
                        Descriptor.Filename, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                    accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pBase);
                    pointerAcquired = true;

                    // PointerOffset accounts for any page-alignment padding added by the OS.
                    // For an offset-0 view it is always 0, but we apply it for correctness.
                    byte* pFileStart = pBase + accessor.PointerOffset;

                    // Validate file format identifier
                    var fileFormatBytes = new byte[FileFormatIdentifierString.Length];
                    for (int i = 0; i < fileFormatBytes.Length; i++)
                        fileFormatBytes[i] = pFileStart[i];

                    if (Encoding.ASCII.GetString(fileFormatBytes, 0, FileFormatIdentifierString.Length) !=
                        FileFormatIdentifierString)
                    {
                        throw new QuadDatabaseVersionException(
                            $"The file {Descriptor.Filename} is not a valid Watney database file");
                    }

                    // Note to self: why wasn't I smart enough to use byte for version number? It's not like there will be many, and the there's endianness...
                    var versionNumBytes = new byte[sizeof(int)];
                    int versionOffset = FileFormatIdentifierString.Length;
                    for (int i = 0; i < sizeof(int); i++)
                        versionNumBytes[i] = pFileStart[versionOffset + i];

                    if (Descriptor.BytesNeedReversing)
                        Array.Reverse(versionNumBytes);

                    var versionNum = BitConverter.ToInt32(versionNumBytes, 0);
                    if (versionNum != FileFormatVersion)
                        throw new QuadDatabaseVersionException(
                            $"Expected database version {FileFormatVersion} format database files, but they were version {versionNum}. " +
                            $"Unable to use them. Make sure you have downloaded the right database files.");

                    _mmf = mmf;
                    _mmvAccessor = accessor;
                    _pFileStart = (nint)pFileStart;
                    _mmfInitialized = true;
                }
                catch (FileNotFoundException)
                {
                    if (pointerAcquired) accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    accessor?.Dispose();
                    mmf?.Dispose();
                    throw new QuadDatabaseException(
                        $"Quad database file {Descriptor.Filename} was not found. Is your quad database intact?");
                }
                catch (Exception e)
                {
                    if (pointerAcquired) accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    accessor?.Dispose();
                    mmf?.Dispose();
                    throw new QuadDatabaseException(
                        $"Failed to read quad database file {Descriptor.Filename}: {e.Message}", e);
                }
            }
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
            int d = pass.SubDivisions;
            var subCells = pass.SubCells;

            for (int r = 0; r < d; r++)
            {
                int rowBase = r * d;

                // Dec pre-filter: all cells in this row share the same Dec center.
                if (Math.Abs(subCells[rowBase].Center.Dec - center.Dec) >= decThreshold)
                    continue;

                var scLeft  = subCells[rowBase];
                var scRight = subCells[rowBase + d - 1];
                bool leftIn  = scLeft.Center.GetAngularDistanceTo(center)  < decThreshold;
                bool rightIn = scRight.Center.GetAngularDistanceTo(center) < decThreshold;

                if (leftIn && rightIn)
                {
                    // Angular distance to the search center is unimodal in RA along a
                    // fixed-Dec row, so the endpoints are the maximum-distance points.
                    // Both endpoints in range means every cell in the row is in range.
                    for (int c = 0; c < d; c++)
                    {
                        subCellsInRangeArr[subCellsInRangeLen]        = subCells[rowBase + c];
                        subCellsInRangeIndexesArr[subCellsInRangeLen] = rowBase + c;
                        subCellsInRangeLen++;
                    }
                    continue;
                }

                // Fan out from the nearest column (minimum angular distance in this row).
                double raStep  = (scRight.Center.Ra - scLeft.Center.Ra) / (d - 1);
                int nearCol = Math.Clamp((int)Math.Round((center.Ra - scLeft.Center.Ra) / raStep), 0, d - 1);

                // If the minimum-distance column fails, all others in this row fail too.
                if (subCells[rowBase + nearCol].Center.GetAngularDistanceTo(center) >= decThreshold)
                    continue;
                subCellsInRangeArr[subCellsInRangeLen]        = subCells[rowBase + nearCol];
                subCellsInRangeIndexesArr[subCellsInRangeLen] = rowBase + nearCol;
                subCellsInRangeLen++;

                // Walk left from nearCol, stopping at first failure.
                for (int c = nearCol - 1; c >= 0; c--)
                {
                    var sc = subCells[rowBase + c];
                    if (sc.Center.GetAngularDistanceTo(center) >= decThreshold) break;
                    subCellsInRangeArr[subCellsInRangeLen]        = sc;
                    subCellsInRangeIndexesArr[subCellsInRangeLen] = rowBase + c;
                    subCellsInRangeLen++;
                }

                // Walk right from nearCol, stopping at first failure.
                for (int c = nearCol + 1; c < d; c++)
                {
                    var sc = subCells[rowBase + c];
                    if (sc.Center.GetAngularDistanceTo(center) >= decThreshold) break;
                    subCellsInRangeArr[subCellsInRangeLen]        = sc;
                    subCellsInRangeIndexesArr[subCellsInRangeLen] = rowBase + c;
                    subCellsInRangeLen++;
                }
            }

            var thisFileCache = cache.Files[_fileId];

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
                    EnsureMmfOpen();

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

                    byte* pQuadData = (byte*)_pFileStart + streamReadOffset;
                    for (var q = 0; q < numberOfQuadsToRead; q++, pQuadData += QuadDataLen)
                    {
                        var quad = BytesToQuadNew(pQuadData, 0, sortedImageQuads, _bytesNeedReversing);
                        if (quad == null)
                            continue;

                        matchingQuads.Add(quad);
                        if (quad.MidPoint.GetAngularDistanceTo(center) < angularDistance)
                            matchingQuadsWithinRange.Add(quad);
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

            return matchingQuadsWithinRange.ToArray();
        }

        private const float OnePer1023 = 0.0009775171065493f; // (1 / 1023)
        private const float OnePer511 = 0.001956947162f; // (1 / 511)
        private const float RatioMatchLow  = 1.0f - 0.011f; // 0.989
        private const float RatioMatchHigh = 1.0f + 0.011f; // 1.011

        /// <summary>
        /// Returns the first index in <paramref name="arr"/> where R0 &gt;= <paramref name="value"/>
        /// (standard lower_bound over a R0-sorted array).
        /// </summary>
        private static int LowerBound(ImageStarQuad[] arr, float value)
        {
            int left = 0, right = arr.Length;
            while (left < right)
            {
                int mid = (left + right) >> 1;
                if (arr[mid].Ratios.R0 < value)
                    left = mid + 1;
                else
                    right = mid;
            }
            return left;
        }

        /// <summary>
        /// Read the bytes and spit out a quad.
        /// </summary>
        /// <param name="pBuf"></param>
        /// <param name="offset"></param>
        /// <param name="sortedTentativeMatches">If given, we only return the constructed quad if the ratios match to one of the sortedTentativeMatches image quads.
        /// The array must be sorted ascending by R0 so that binary search can be used.
        /// Otherwise do no matching and just read the quad from the buffer and return it.</param>
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

            // Binary search into the R0-sorted array to skip the ~90% of quads outside the R0 window.
            int startIdx = LowerBound(sortedTentativeMatches, lo.R0);
            for (var q = startIdx; q < sortedTentativeMatches.Length; q++)
            {
                var imgQuad = sortedTentativeMatches[q];
                if (imgQuad.Ratios.R0 > hi.R0) break; // past the R0 window; array is sorted
                if (imgQuad.Ratios.R1 < lo.R1 || imgQuad.Ratios.R1 > hi.R1) continue;
                if (imgQuad.Ratios.R2 < lo.R2 || imgQuad.Ratios.R2 > hi.R2) continue;
                if (imgQuad.Ratios.R3 < lo.R3 || imgQuad.Ratios.R3 > hi.R3) continue;
                if (imgQuad.Ratios.R4 < lo.R4 || imgQuad.Ratios.R4 > hi.R4) continue;
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

            return null;
        }

        public unsafe void Dispose()
        {
            if (_mmfInitialized)
                _mmvAccessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _mmvAccessor?.Dispose();
            _mmf?.Dispose();
        }
    }
}
