namespace WatneyAstrometry.Core.Types
{
    public interface IStar
    {
        double CalculateDistance(IStar anotherStar);
    }
}