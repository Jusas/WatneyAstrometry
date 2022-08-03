using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Animation.Easings;
using Avalonia.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;

namespace WatneyAstrometry.SolverVizTools.Drawing;

public class HistogramTransformation<TPixel> : ImageProcessor<TPixel>, IImageProcessor where TPixel : unmanaged, IPixel<TPixel>
{

    private readonly struct GrayscaleLevelsRowOperation : IRowOperation
    {
        private readonly Rectangle bounds;
        private readonly IMemoryOwner<int> histogramBuffer;
        private readonly Buffer2D<TPixel> source;
        private readonly int luminanceLevels;
        
        public GrayscaleLevelsRowOperation(
            Rectangle bounds,
            IMemoryOwner<int> histogramBuffer,
            Buffer2D<TPixel> source,
            int luminanceLevels)
        {
            this.bounds = bounds;
            this.histogramBuffer = histogramBuffer;
            this.source = source;
            this.luminanceLevels = luminanceLevels;
        }
        
        private static Vector4 _bt709 = new Vector4(.2126f, .7152f, .0722f, 0.0f);
        public void Invoke(int y)
        {
            ref int histogramBase = ref MemoryMarshal.GetReference(this.histogramBuffer.Memory.Span);
            Span<TPixel> pixelRow = this.source.DangerousGetRowSpan(y);
            int levels = this.luminanceLevels;

            for (int x = 0; x < this.bounds.Width; x++)
            {
                var vector = pixelRow[x].ToVector4();
                var luminance = (int)MathF.Round(Vector4.Dot(vector, _bt709) * (levels - 1));
                Interlocked.Increment(ref Unsafe.Add(ref histogramBase, luminance));
            }
        }
    }

    private readonly struct ApplyStretchRowOperation : IRowOperation
    {
        private readonly Rectangle bounds;
        private readonly float midtones;
        private readonly float shadows;
        private readonly Buffer2D<TPixel> source;
        private readonly int luminanceLevels;
        private readonly object _lock = new object();

        private static Vector4 _bt709 = new Vector4(.2126f, .7152f, .0722f, 0.0f);
        private readonly Easing easing;

        public ApplyStretchRowOperation(
            Rectangle bounds,
            Buffer2D<TPixel> source,
            float shadows,
            float midtones,
            int luminanceLevels,
            Easing easing)
        {
            this.bounds = bounds;
            this.shadows = shadows;
            this.midtones = midtones;
            this.source = source;
            this.luminanceLevels = luminanceLevels;
            this.easing = easing;
        }
        
        public void Invoke(int y)
        {
            Span<TPixel> pixelRow = this.source.DangerousGetRowSpan(y);
            int levels = this.luminanceLevels;
            
            for (int x = 0; x < this.bounds.Width; x++)
            {
                
                ref TPixel pixel = ref pixelRow[x];
                var vector = pixel.ToVector4();
                var luminance = (int)MathF.Round(Vector4.Dot(vector, _bt709) * (levels - 1));
                var luminanceNormalized = (float)luminance / luminanceLevels;
                var luminanceFactor = 1.0f;
                
                float val = 0;
                lock(_lock)
                    val = (float)easing.Ease(luminanceNormalized);

                pixel.FromVector4(new Vector4(val, val, val, vector.W));
            }
        }
    }

    // 256, 65536, ...
    public int LuminanceLevels { get; }

    public HistogramTransformation(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle) : base(configuration, source, sourceRectangle)
    {
        this.LuminanceLevels = typeof(TPixel) == typeof(Rgba32)
            ? 256
            : typeof(TPixel) == typeof(L16)
                ? 65536
                : 256;
    }

    private (int median, int mad) GetMedianAndMad(Span<int> histogram)
    {
        var histogramSorted = new (int lum, int count)[histogram.Length];
        for (var i = 0; i < histogram.Length; i++)
        {
            histogramSorted[i] = (i, histogram[i]);
        }
        Array.Sort(histogramSorted, (a, b) => a.count < b.count ? -1 : 1);

        var median = histogramSorted[histogramSorted.Length / 2].lum;

        for (var i = 0; i < histogramSorted.Length; i++)
        {
            histogramSorted[i].lum = Math.Abs(histogramSorted[i].lum - median);
        }
        Array.Sort(histogramSorted, (a, b) => a.lum < b.lum ? -1 : 1);

        var mad = histogramSorted[histogramSorted.Length / 2].lum;
        
        return (median, mad);
    }

    protected override void OnFrameApply(ImageFrame<TPixel> source)
    {

        MemoryAllocator memoryAllocator = this.Configuration.MemoryAllocator;
        int numberOfPixels = source.Width * source.Height;
        var interest = Rectangle.Intersect(this.SourceRectangle, source.Bounds());

        using IMemoryOwner<int> histogramBuffer = memoryAllocator.Allocate<int>(this.LuminanceLevels, AllocationOptions.Clean);

        // Build the histogram of the grayscale levels.
        var grayscaleOperation = new GrayscaleLevelsRowOperation(interest, histogramBuffer, source.PixelBuffer, this.LuminanceLevels);
        ParallelRowIterator.IterateRows(
            this.Configuration,
            interest,
            in grayscaleOperation);

        Span<int> histogram = histogramBuffer.Memory.Span;
        var histogramSorted = new (int lum, int count)[histogram.Length];
        for (var i = 0; i < histogram.Length; i++)
        {
            histogramSorted[i] = (i, histogram[i]);
        }
        Array.Sort(histogramSorted, (a, b) => a.count < b.count ? -1 : 1);

        var (median, mad) = GetMedianAndMad(histogram);

        var dMedian = (double)median / LuminanceLevels;
        var dMad = (double)mad / LuminanceLevels;
        dMad *= 1.4826;

        var c0 = dMedian + (-3.8) * dMad;
        var m = dMad;
        
        // clamp
        c0 = Math.Max(0.0, Math.Min(c0, 1.0));

        var midtones = m - c0;
        var shadows = c0;

        // Well, since Avalonia provides us with splines, let's make use of that.
        // Ugh, this is not nice at all but... using a single SplineEasing instance is not thread safe, plus Avalonia
        // has protections that KeySpline can't be created outside UI thread, overly cautious.
        // I know this is a bad way to do it, plus the lock in ApplyStretchRoeOperation even further slows it down but hey,
        // right now it works and is good enough for the needs.
        // Moving on.
        Easing easing = null;
        easing = Dispatcher.UIThread.InvokeAsync(() => new SplineEasing(shadows, 0, midtones, 0.5f)).Result;
        
        var applyStretchOp =
            new ApplyStretchRowOperation(source.Bounds(), source.PixelBuffer, (float)shadows, (float)midtones, LuminanceLevels, easing);
        ParallelRowIterator.IterateRows(
            this.Configuration,
            interest,
            in applyStretchOp);
    }

    public IImageProcessor<TPixel1> CreatePixelSpecificProcessor<TPixel1>(Configuration configuration, Image<TPixel1> source,
        Rectangle sourceRectangle) where TPixel1 : unmanaged, IPixel<TPixel1>
    {
        return new HistogramTransformation<TPixel1>(configuration, source, sourceRectangle);
    }
}