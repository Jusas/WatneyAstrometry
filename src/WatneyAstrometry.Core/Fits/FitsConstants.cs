#pragma warning disable CS1591
namespace WatneyAstrometry.Core.Fits
{
    public static class FitsConstants
    {
        // The header comes in blocks, one or more of these.
        public const int HeaderBlockSize = 2880;
        // One header record size.
        public const int HduHeaderRecordSize = 80;
    }
}