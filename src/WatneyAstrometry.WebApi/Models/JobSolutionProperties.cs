namespace WatneyAstrometry.WebApi.Models
{
    public class JobSolutionProperties
    {
        public double Ra { get; set; }
        public double Dec { get; set; }
        public double FieldRadius { get; set; }
        public double Orientation { get; set; }
        public double PixScale { get; set; }
        public string Parity { get; set; }
        public double TimeSpent { get; set; }
        public int SearchIterations { get; set; }
        public int QuadMatches { get; set; }

        public JobSolutionFitsWcs FitsWcs { get; set; }
    }

    public class JobSolutionFitsWcs
    {
        public double Cd1_1 { get; set; }
        public double Cd1_2 { get; set; }
        public double Cd2_1 { get; set; }
        public double Cd2_2 { get; set; }
        public double Cdelt1 { get; set; }
        public double Cdelt2 { get; set; }
        public double Crota1 { get; set; }
        public double Crota2 { get; set; }
        public double Crpix1 { get; set; }
        public double Crpix2 { get; set; }
        public double Crval1 { get; set; }
        public double Crval2 { get; set; }
    }
}
