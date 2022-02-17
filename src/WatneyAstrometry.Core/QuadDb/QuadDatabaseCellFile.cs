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
        private static readonly int QuadDataLen = /*ratios*/ 5 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;

        private ConcurrentQueue<FileStream> _fileStreamPool = new ConcurrentQueue<FileStream>();

        private readonly bool _bytesNeedReversing = false;
        
        public QuadDatabaseCellFile(QuadDatabaseCellFileDescriptor descriptor)
        {
            Descriptor = descriptor;
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
        public StarQuad[] GetQuads(EquatorialCoords center, double angularDistance, int passIndex, int numSubSets, int subSetIndex, ImageStarQuad[] imageQuads)
        {
            var foundQuads = new List<StarQuad>();
            var subCellsInRange = new List<QuadDatabaseCellFileDescriptor.SubCellInfo>();

            var pass = Descriptor.Passes[passIndex];

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
                fileStream = new FileStream(Descriptor.Filename, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                    var quad = BytesToQuadNew(dataBuf, advance, imageQuadsCopy, _bytesNeedReversing);
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
        /// <param name="tentativeMatches">If given, we only return the constructed quad if the ratios match to one of the tentativeMatches image quads.
        /// Otherwise do no matching and just read the quad from the buffer and return it.</param>
        /// <param name="bytesNeedReversing">Flag that indicates if we need to reverse byte order because of endianness difference between DB file contents and the system</param>
        /// <returns></returns>
        private static unsafe StarQuad BytesToQuadNew(byte[] buf, int offset, ImageStarQuad[] tentativeMatches, bool bytesNeedReversing)
        {
            // Optimized: we try to detect unfit quads as early as possible, and we try to
            // do as little work as possible in order to achieve that. We can shave off some seconds
            // from blind solves by doing this.

            bool noMatching = tentativeMatches == null;
            fixed (byte* pBuf = buf)
            {
                
                byte* pRatios = (byte*)(pBuf + offset);

                float* pFloats = (float*)(pBuf + offset + 5); // floats come after the 5 byte-sized ratios
                float largestDist;
                float ra;
                float dec;
                
                if (noMatching)
                {
                    var ratios = new[] { pRatios[0] / 255.0f, pRatios[1] / 255.0f, pRatios[2] / 255.0f, pRatios[3] / 255.0f, pRatios[4] / 255.0f };
                    
                    if (bytesNeedReversing)
                    {
                        byte* pFloatBytes = (byte*)(pBuf + offset + 5);
                        largestDist = BitConverter.ToSingle(
                        new byte[] { pFloatBytes[3], pFloatBytes[2], pFloatBytes[1], pFloatBytes[0] }, 0);
                        ra = BitConverter.ToSingle(
                            new byte[] { pFloatBytes[7], pFloatBytes[6], pFloatBytes[5], pFloatBytes[4] }, 0);
                        dec = BitConverter.ToSingle(
                            new byte[] { pFloatBytes[11], pFloatBytes[10], pFloatBytes[9], pFloatBytes[8] }, 0);
                    }
                    else
                    {
                        largestDist = pFloats[0];
                        ra = pFloats[1];
                        dec = pFloats[2];
                    }
                    

                    var quad = new StarQuad(ratios, largestDist, new EquatorialCoords(ra, dec));
                    return quad;
                }

                for (var q = 0; q < tentativeMatches.Length; q++)
                {
                    // TODO: We're no longer setting tentativeMatches[q] to null and resetting the array in the calling method, why was this removed originally? Should inspect performance...
                    var imgQuad = tentativeMatches[q];
                    if (imgQuad != null
                        && Math.Abs(imgQuad.Ratios[0] / (pRatios[0] / 255.0f) - 1.0f) <= 0.015f
                        && Math.Abs(imgQuad.Ratios[1] / (pRatios[1] / 255.0f) - 1.0f) <= 0.015f
                        && Math.Abs(imgQuad.Ratios[2] / (pRatios[2] / 255.0f) - 1.0f) <= 0.015f
                        && Math.Abs(imgQuad.Ratios[3] / (pRatios[3] / 255.0f) - 1.0f) <= 0.015f
                        && Math.Abs(imgQuad.Ratios[4] / (pRatios[4] / 255.0f) - 1.0f) <= 0.015f
                    )
                    {
                        var ratios = new[] { pRatios[0] / 255.0f, pRatios[1] / 255.0f, pRatios[2] / 255.0f, pRatios[3] / 255.0f, pRatios[4] / 255.0f };

                        if (bytesNeedReversing)
                        {
                            byte* pFloatBytes = (byte*)(pBuf + offset + 5);
                            largestDist = BitConverter.ToSingle(
                                new byte[] { pFloatBytes[3], pFloatBytes[2], pFloatBytes[1], pFloatBytes[0] }, 0);
                            ra = BitConverter.ToSingle(
                                new byte[] { pFloatBytes[7], pFloatBytes[6], pFloatBytes[5], pFloatBytes[4] }, 0);
                            dec = BitConverter.ToSingle(
                                new byte[] { pFloatBytes[11], pFloatBytes[10], pFloatBytes[9], pFloatBytes[8] }, 0);
                        }
                        else
                        {
                            largestDist = pFloats[0];
                            ra = pFloats[1];
                            dec = pFloats[2];
                        }

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