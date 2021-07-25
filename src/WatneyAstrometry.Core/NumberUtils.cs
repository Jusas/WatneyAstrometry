namespace WatneyAstrometry.Core
{
    public static class NumberUtils
    {
        public static double Deg2ArcMin(this double degrees) => degrees * 60;
        public static double ArcMin2Deg(this double arcmins) => arcmins / 60;

    }
}