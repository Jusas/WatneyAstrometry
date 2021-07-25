using System;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Image's metadata, the field size.
    /// </summary>
    public class ViewArea
    {
        public double WidthDeg { get; set; }
        public double HeightDeg { get; set; }
        public double DiameterDeg => Math.Sqrt(WidthDeg * WidthDeg + HeightDeg * HeightDeg);
    }
}