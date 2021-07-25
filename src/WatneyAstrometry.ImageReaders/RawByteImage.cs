using System.IO;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.ImageReaders
{
    public class RawByteImage : IImage
    {
        /// <summary>
        /// Opens an image from disk, which is a raw byte stream.
        /// </summary>
        /// <param name="filename">The file to load data from</param>
        /// <param name="deleteInDispose">Deletes the file when disposing (USE THIS ONLY WHEN USING TEMPORARY FILES!)</param>
        public RawByteImage(string filename, bool deleteInDispose)
        {
            _deleteInDispose = deleteInDispose;
            _filename = filename;
            PixelDataStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Dispose()
        {
            PixelDataStream?.Dispose();
            if(_deleteInDispose)
                File.Delete(_filename);
        }

        private bool _deleteInDispose;
        private string _filename;

        public Stream PixelDataStream { get; }
        public long PixelDataStreamOffset => 0;
        public long PixelDataStreamLength => PixelDataStream.Length;
        public Metadata Metadata { get; internal set; }
    }
}