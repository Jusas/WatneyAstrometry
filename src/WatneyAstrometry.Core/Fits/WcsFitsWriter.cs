using System;
using System.Globalization;
using System.IO;
using System.Text;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.Fits
{
    /// <summary>
    /// Writes a FITS file that holds WCS coordinates in
    /// the headers but contains no actual data (a WCS file).
    /// </summary>
    public class WcsFitsWriter
    {
        private readonly Stream _output;
        private int _bytesWritten = 0;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="output"></param>
        public WcsFitsWriter(Stream output)
        {
            _output = output;
        }

        /// <summary>
        /// Write a solution WCS contents into the assigned stream.
        /// </summary>
        /// <param name="fitsHeaders"></param>
        /// <param name="imageDimensions"></param>
        public void WriteWcsFile(Solution.FitsHeaderFields fitsHeaders, IImageDimensions imageDimensions)
        {
            WriteWcsFile(fitsHeaders, imageDimensions.ImageWidth, imageDimensions.ImageHeight);
        }

        /// <summary>
        /// Write a solution WCS contents into the assigned stream.
        /// </summary>
        /// <param name="fitsHeaders"></param>
        /// <param name="imageW"></param>
        /// <param name="imageH"></param>
        public void WriteWcsFile(Solution.FitsHeaderFields fitsHeaders, int imageW, int imageH)
        {
            // See https://fits.gsfc.nasa.gov/standard40/fits_standard40aa-le.pdf

            WriteRecord("SIMPLE", 'T', "Standard FITS file");
            WriteRecord("BITPIX", '8', "no real data");
            WriteRecord("NAXIS", '0', "no real data");
            WriteRecord("EXTEND", 'T', "no comment");
            WriteRecord("WCSAXES", '2', "ra and dec");
            WriteRecord("CTYPE1", "RA---TAN", "linear");
            WriteRecord("CTYPE2", "DEC--TAN", "linear");
            WriteRecord("EQUINOX", 2000, "Equatorial coords definition year");
            WriteRecord("LONPOLE", 180.0, "no comment");
            WriteRecord("LATPOLE", 0.0, "no comment");
            WriteRecord("CRVAL1", fitsHeaders.CRVAL1, comment: "RA of reference pixel");
            WriteRecord("CRVAL2", fitsHeaders.CRVAL2, comment: "DEC of reference pixel");
            WriteRecord("CRPIX1", fitsHeaders.CRPIX1, comment: "X of reference pixel");
            WriteRecord("CRPIX2", fitsHeaders.CRPIX2, comment: "Y of reference pixel");
            WriteRecord("CUNIT1", "deg", "degrees");
            WriteRecord("CUNIT2", "deg", "degrees");
            WriteRecord("CD1_1", fitsHeaders.CD1_1, "cd matrix");
            WriteRecord("CD1_2", fitsHeaders.CD1_2, "cd matrix");
            WriteRecord("CD2_1", fitsHeaders.CD2_1, "cd matrix");
            WriteRecord("CD2_2", fitsHeaders.CD2_2, "cd matrix");
            WriteRecord("IMAGEW", imageW, "Image width in pixels");
            WriteRecord("IMAGEH", imageH, "Image height in pixels");
            WritePureComment("WCS header created by Watney Astrometry");
            WritePureComment("https://github.com/Jusas/WatneyAstrometry");
            WriteEnd();
        }

        internal void WriteEnd()
        {
            var end = "END".PadRight(80);
            var recordBytes = Encoding.ASCII.GetBytes(end);
            _output.Write(recordBytes, 0, recordBytes.Length);
            _bytesWritten += recordBytes.Length;

            if (_bytesWritten % 2880 > 0)
            {
                var bytesToFill = 2880 - (_bytesWritten % 2880);
                var spaces = "".PadRight(bytesToFill);
                recordBytes = Encoding.ASCII.GetBytes(spaces);
                _output.Write(recordBytes, 0, recordBytes.Length);
                _bytesWritten += recordBytes.Length;
            }
            
        }

        internal void WritePureComment(string comment)
        {
            var commentChars = $"COMMENT {comment}"
                .PadRight(80)
                .Substring(0, 80);

            var record = commentChars;
            var recordBytes = Encoding.ASCII.GetBytes(record);
            _output.Write(recordBytes, 0, recordBytes.Length);
            _bytesWritten += recordBytes.Length;
        }

        internal void WriteRecord(string keyword, object value, string comment = null)
        {
            var keywordChars = keyword
                .PadRight(8)
                .Substring(0, 8);
            var valueIndicator = "= ";

            string valueChars = "";

            if (value == null)
                valueChars = "".PadRight(70);
            else if (value is double d)
                valueChars = d.ToString("G13", CultureInfo.InvariantCulture) // Ensure we don't get too long values
                    .PadLeft(20)
                    .Substring(0, 20);
            else if (value is int i)
                valueChars = $"{i}"
                    .PadLeft(20)
                    .Substring(0, 20);
            else if (value is char c)
                valueChars = $"{c}"
                    .PadLeft(20)
                    .Substring(0, 20);

            else if (value is string s)
                valueChars = $"'{s.PadRight(8)}'";
            
            if (comment != null)
            {
                valueChars = $"{valueChars} / {comment}";
            }

            valueChars = valueChars
                .PadRight(70)
                .Substring(0, 70);

            var record = keywordChars + valueIndicator + valueChars;
            var recordBytes = Encoding.ASCII.GetBytes(record);
            _output.Write(recordBytes, 0, recordBytes.Length);
            _bytesWritten += recordBytes.Length;
        }
        
    }
}