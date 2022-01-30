using System.Globalization;
using WatneyAstrometry.Core.QuadDb;

namespace WatneyAstrometry.QuadDatabaseInspector
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            var filename = args.Last();

            if (!File.Exists(filename))
            {
                Console.WriteLine("File does not exist");
                return;
            }

            var cellFile = new QuadDatabaseCellFile(filename);
            var d = cellFile.FileDescriptor;

            Console.WriteLine($"Cell id: {d.CellId}");
            Console.WriteLine($"Number of passes: {d.Passes.Length}");
            Console.WriteLine($"Pass quad densities: {string.Join(", ", d.Passes.Select(x => x.QuadsPerSqDeg.ToString(CultureInfo.InvariantCulture)))}");
        }
    }
}