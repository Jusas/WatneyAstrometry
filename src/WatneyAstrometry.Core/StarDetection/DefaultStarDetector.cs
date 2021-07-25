using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.Core.StarDetection
{
    /// <summary>
    /// Default implementation of the star detector.<br/>
    /// Note: Only supports monochrome data streams. If you wish to use a color image, you must
    /// first convert it to monochrome.
    /// <para>
    /// A star detector reads a source image and sweeps it looking for shapes that
    /// could be interpreted as stars. This implementation reads the (monochrome) image scanline by
    /// scanline from top to bottom and gathers pixels that go over the noise threshold
    /// of the image, and then joins them together with adjacent detected pixels to form
    /// "bins" of star pixels, each bin representing a star. A filter (see <see cref="IStarDetectionFilter"/>)
    /// is then applied to the bins to discard anomalies like streaks and large blobs.
    /// </para>
    /// <para>
    /// The detection algorithm tries to be simple and efficient, with "good enough" being
    /// the end goal.
    /// </para>
    /// </summary>
    public class DefaultStarDetector : IStarDetector
    {
        private Stream _imageDataStream;
        private Metadata _imageMetadata;
        private long _streamDataPos;

        private Dictionary<long, long> _histogram;
        private long _histogramPeakValue = 0;
        private long _histogramPeakCount = 0;
        private int _bytesPerPixel;

        private List<StarPixelBin> _starBins = new List<StarPixelBin>();
        internal IReadOnlyList<StarPixelBin> StarBins => _starBins;

        public IStarDetectionFilter DetectionFilter { get; set; } = new DefaultStarDetectionFilter();

        public long PixelValueRangeStart => 0;

        public DefaultStarDetector()
        {
        }


        private void ValidateInputArgs()
        {
            var supportedBpp = new[] {8, 16, 32};
            if(!supportedBpp.Contains(_imageMetadata.BitsPerPixel))
                throw new NotSupportedException($"Unsupported BPP value '{_imageMetadata.BitsPerPixel}'. Supported BPP values are: {string.Join(", ", supportedBpp)}");
        }

        private void Initialize(IImage image)
        {
            _imageDataStream = image.PixelDataStream ?? throw new ArgumentNullException(nameof(image.PixelDataStream));
            _imageMetadata = image.Metadata ?? throw new ArgumentNullException(nameof(image.Metadata));
            _streamDataPos = image.PixelDataStreamOffset;
            _bytesPerPixel = _imageMetadata.BitsPerPixel / 8;
            ValidateInputArgs();
        }

        public IList<ImageStar> DetectStars(IImage image)
        {
            Initialize(image);

            _imageDataStream.Seek(_streamDataPos, SeekOrigin.Begin);
            if(_histogram == null || !_histogram.Any())
                CreateHistogram();

            _imageDataStream.Seek(_streamDataPos, SeekOrigin.Begin);
            byte[] buf = new byte[_imageMetadata.ImageWidth * _bytesPerPixel];


            var pixelCount = _imageMetadata.ImageWidth * _imageMetadata.ImageHeight;
            var pixelSum = _histogram.Sum(x => x.Key * x.Value);
            var pixelAvg = pixelSum / pixelCount;
            double diffSquared = _histogram.Sum(x => (x.Key - pixelAvg) * (x.Key - pixelAvg) * x.Value);
            double stdDev = Math.Sqrt(diffSquared / pixelCount);

            long flatValue = pixelAvg + (long)(stdDev * 3);

            List<StarPixelBin> previousLineBins = new List<StarPixelBin>();
            for (var y = 0; y < _imageMetadata.ImageHeight; y++)
            {
                _imageDataStream.Read(buf, 0, buf.Length);
                previousLineBins = BinStarPixelsFromScanline(buf, y, flatValue, previousLineBins);
            }

            for (var i = 0; i < _starBins.Count; i++)
                _starBins[i].RecalcLeftRightTopBottom();

            _starBins = DetectionFilter.ApplyFilter(_starBins, _imageMetadata);

            return _starBins.Select(x =>
            {
                var starProps = x.GetCenterPixelPosAndRelativeBrightness();
                return new ImageStar(starProps.PixelPosX, starProps.PixelPosY, starProps.BrightnessValue);
            }).ToList();



        }

        private void CreateHistogram()
        {
            _histogram = new Dictionary<long, long>();
            _imageDataStream.Seek(_streamDataPos, SeekOrigin.Begin);

            byte[] buf = new byte[_imageMetadata.ImageWidth * _imageMetadata.BitsPerPixel / 8];

            for (var y = 0; y < _imageMetadata.ImageHeight; y++)
            {
                _imageDataStream.Read(buf, 0, buf.Length);
                AddScanlineToHistogram(buf);
            }
        }


        private unsafe void AddScanlineToHistogram(byte[] bytes)
        {
            var byteIncrement = _imageMetadata.BitsPerPixel / 8;
            var byteWidth = _imageMetadata.ImageWidth * _imageMetadata.BitsPerPixel / 8;

            fixed (byte* pBuffer = bytes)
            {
                for (int pos = 0; pos < byteWidth; pos += byteIncrement)
                {
                    long count = 0;
                    if (_imageMetadata.BitsPerPixel == 8)
                    {
                        _histogram.TryGetValue(pBuffer[pos], out count);
                        _histogram[pBuffer[pos]] = ++count;
                        if (count > _histogramPeakCount)
                        {
                            _histogramPeakCount = count;
                            _histogramPeakValue = pBuffer[pos];
                        }
                    }
                    else if (_imageMetadata.BitsPerPixel == 16)
                    {
                        short val = (short)(pBuffer[pos] << 8 | pBuffer[pos + 1]);
                        ushort uval = (ushort)(val - short.MinValue);
                        _histogram.TryGetValue(uval, out count);
                        _histogram[uval] = ++count;
                        if (count > _histogramPeakCount)
                        {
                            _histogramPeakCount = count;
                            _histogramPeakValue = uval;
                        }
                    }
                    else if (_imageMetadata.BitsPerPixel == 32)
                    {
                        int val = (int)(pBuffer[pos] << 24 | pBuffer[pos + 1] << 16 | pBuffer[pos + 2] << 8 |
                                           pBuffer[pos + 3]);
                        uint uval = (ushort)(val - int.MinValue);
                        _histogram.TryGetValue(uval, out count);
                        _histogram[uval] = ++count;
                        if (count > _histogramPeakCount)
                        {
                            _histogramPeakCount = count;
                            _histogramPeakValue = uval;
                        }
                    }
                }
            }
        }


        // Algorithm: read whole line into star pixel bins (contiguous pixels over background value on X axis).
        // Then look up one row (x-1 and x+1) for previous line bins. Combine the current bin to that/them
        // (check from left to right, combine self with topleftmost, and potentially the topright with topleftmost too)
        private unsafe List<StarPixelBin> BinStarPixelsFromScanline(byte[] bytes, int y, long flatValue, List<StarPixelBin> previousLineBins)
        {
            StarPixelBin currentBin = null;
            List<StarPixelBin> scanLineBins = new List<StarPixelBin>();

            // Collect the current pixel line into contiguous star pixel bins.
            fixed (byte* pBuffer = bytes)
            {
                var byteIncrement = _bytesPerPixel;
                var scanlineByteLen = _imageMetadata.ImageWidth * _bytesPerPixel;
                for (int pos = 0, x = 0; pos < scanlineByteLen; pos += byteIncrement, x++)
                {
                    if (_imageMetadata.BitsPerPixel == 8)
                    {
                        byte val = pBuffer[pos];
                        if (val > flatValue)
                        {
                            if (currentBin == null)
                            {
                                currentBin = new StarPixelBin(x, y, val);
                                scanLineBins.Add(currentBin);
                            }
                            else
                                currentBin.Add(x, y, pBuffer[pos]);
                        }
                        else
                        {
                            currentBin = null;
                        }
                    }
                    else if (_imageMetadata.BitsPerPixel == 16)
                    {
                        short val = (short)(pBuffer[pos] << 8 | pBuffer[pos + 1]);
                        ushort uval = (ushort)(val - short.MinValue);
                        if (uval > flatValue)
                        {
                            if (currentBin == null)
                            {
                                currentBin = new StarPixelBin(x, y, uval);
                                scanLineBins.Add(currentBin);
                            }
                            else
                                currentBin.Add(x, y, uval);
                        }
                        else
                        {
                            currentBin = null;
                        }
                    }
                    else if (_imageMetadata.BitsPerPixel == 32)
                    {
                        int val = (int)(pBuffer[pos] << 24 | pBuffer[pos + 1] << 16 | pBuffer[pos + 2] << 8 |
                                          pBuffer[pos + 3]);
                        uint uval = (uint)(val - int.MinValue);
                        if (uval > flatValue)
                        {
                            if (currentBin == null)
                            {
                                currentBin = new StarPixelBin(x, y, uval);
                                scanLineBins.Add(currentBin);
                            }
                            else
                                currentBin.Add(x, y, uval);
                        }
                        else
                        {
                            currentBin = null;
                        }
                    }
                }
            }

            // No combination to above star pixel bins required if none were present.
            if (previousLineBins.Count == 0)
            {
                _starBins.AddRange(scanLineBins);
                return scanLineBins;
            }

            // Merge into previous line's star pixel bins if they happen to be adjacent.

            // Find the ones above (+-1 px l/r)
            // Take first
            // Add our pixels to that one
            // Add the others' pixels to that one

            List<StarPixelBin> rowOutputBins = new List<StarPixelBin>();

            for (var i = 0; i < scanLineBins.Count; i++)
            {
                var starBin = scanLineBins[i];
                var left = starBin.Left;
                var right = starBin.Right;
                bool merged = false;

                List<StarPixelBin> connectedPreviousLinePixelBins = new List<StarPixelBin>();

                for (var j = 0; j < previousLineBins.Count; j++)
                {
                    var prevLineBin = previousLineBins[j];
                    var prevLineBinPixels = prevLineBin.PixelRows[y - 1];
                    for (var p = 0; p < prevLineBinPixels.Count; p++)
                    {
                        if (prevLineBinPixels[p].X >= left - 1 && prevLineBinPixels[p].X <= right + 1)
                        {
                            connectedPreviousLinePixelBins.Add(prevLineBin);
                            break;
                        }
                    }
                }

                if (connectedPreviousLinePixelBins.Count > 0)
                {
                    merged = true;
                    var mergeTarget = connectedPreviousLinePixelBins[0];
                    if (!mergeTarget.PixelRows.ContainsKey(y))
                        mergeTarget.PixelRows[y] = new List<StarPixel>(starBin.PixelRows[y]);
                    else
                        mergeTarget.PixelRows[y].AddRange(starBin.PixelRows[y]);

                    if (!rowOutputBins.Contains(mergeTarget))
                        rowOutputBins.Add(mergeTarget);

                    for (var n = 1; n < connectedPreviousLinePixelBins.Count; n++)
                    {
                        var mergeable = connectedPreviousLinePixelBins[n];
                        foreach (var pixelRow in mergeable.PixelRows)
                        {
                            var k = pixelRow.Key;
                            if (!mergeTarget.PixelRows.ContainsKey(k))
                                mergeTarget.PixelRows[k] = new List<StarPixel>(pixelRow.Value);
                            else
                                mergeTarget.PixelRows[k].AddRange(pixelRow.Value);
                        }

                        _starBins.Remove(mergeable); // Remove, since this is now merged with another one.
                        previousLineBins.Remove(mergeable);
                    }
                }

                if (!merged)
                {
                    _starBins.Add(starBin);
                    rowOutputBins.Add(starBin);
                }
            }

            return rowOutputBins;
        }


    }
}