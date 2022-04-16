using System.IO;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.ImageReaders
{
    /// <summary>
    /// A raw byte image file (pixel buffer).
    /// </summary>
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

        /// <summary>
        /// Disposes the image, and deletes the file as well if it was marked to be deleted on dispose.
        /// </summary>
        public void Dispose()
        {
            PixelDataStream?.Dispose();
            if(_deleteInDispose)
                File.Delete(_filename);
        }

        private bool _deleteInDispose;
        private string _filename;

        /// <summary>
        /// The byte stream.
        /// </summary>
        public Stream PixelDataStream { get; }
        /// <summary>
        /// Data offset.
        /// </summary>
        public long PixelDataStreamOffset => 0;
        /// <summary>
        /// Length of the pixel data.
        /// </summary>
        public long PixelDataStreamLength => PixelDataStream.Length;
        /// <summary>
        /// Image metadata.
        /// </summary>
        public Metadata Metadata { get; internal set; }
    }
}