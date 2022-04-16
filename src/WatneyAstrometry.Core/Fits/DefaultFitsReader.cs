// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.Fits
{
    /// <summary>
    /// The default built-in implementation for reading FITS files for solving.
    /// This is a very, very simplified implementation that supports only 8, 16 and 32 bit
    /// monochrome FITS images. It exists in order to not add any extra dependencies
    /// and works for the most common use cases.
    /// <para>
    /// Alternative implementations can be written and used instead when needed.
    /// </para>
    /// </summary>
    public class DefaultFitsReader : IImageReader
    {
        /// <summary>
        /// Checks if the file is supported by this reader.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsSupported(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                return IsSupported(stream);
        }

        /// <summary>
        /// Checks if the data stream is supported by this reader.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool IsSupported(Stream stream)
        {
            var pos = stream.Position;
            if (stream.Length < FitsConstants.HeaderBlockSize)
                return false;

            var buf = new byte[FitsConstants.HeaderBlockSize];
            stream.Read(buf, 0, FitsConstants.HeaderBlockSize);
            stream.Seek(pos, SeekOrigin.Begin);

            try
            {
                ValidateBegin(buf);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public IImage FromFile(string filename)
        {
            var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            return BuildFitsImage(stream);
        }

        /// <inheritdoc />
        public IImage FromStream(Stream stream)
        {
            return BuildFitsImage(stream);
        }
        
        private FitsImage BuildFitsImage(Stream stream)
        {
            var fitsImage = new FitsImage()
            {
                HduHeaderRecords = new List<HduHeaderRecord>(),
                Metadata = new Metadata()
            };

            stream.Seek(0, SeekOrigin.Begin);
            if (stream.Length < FitsConstants.HeaderBlockSize)
                throw new Exception($"A valid FITS file header should be at least {FitsConstants.HeaderBlockSize} bytes");

            int headerBlockIndex = 0;
            var buf = new byte[FitsConstants.HeaderBlockSize];

            // Keep reading header blocks until we run into "END" header.
            while (stream.Read(buf, 0, FitsConstants.HeaderBlockSize) == FitsConstants.HeaderBlockSize)
            {
                if (headerBlockIndex == 0)
                    ValidateBegin(buf);

                for (var i = 0; i < FitsConstants.HeaderBlockSize / FitsConstants.HduHeaderRecordSize; i++)
                {
                    var hduRecord = new HduHeaderRecord(Encoding.ASCII.GetString(buf, i * FitsConstants.HduHeaderRecordSize, FitsConstants.HduHeaderRecordSize));
                    if (hduRecord.Keyword == "END")
                    {
                        PopulateDimensions(fitsImage);
                        ThrowIfUnsupportedFormat(fitsImage);
                        fitsImage.PixelDataStreamOffset = FitsConstants.HeaderBlockSize + headerBlockIndex * FitsConstants.HeaderBlockSize;
                        fitsImage.PixelDataStream = stream;
                        fitsImage.PixelDataStreamLength = fitsImage.Metadata.ImageWidth *
                            fitsImage.Metadata.ImageHeight * fitsImage.Metadata.BitsPerPixel / 8;
                        stream.Seek(fitsImage.PixelDataStreamOffset, SeekOrigin.Begin);
                        return fitsImage;
                    }

                    fitsImage.HduHeaderRecords.Add(hduRecord);
                }

                headerBlockIndex++;
            }

            throw new Exception("HDU with keyword END was not found");
        }

        private static void ValidateBegin(byte[] headerData)
        {
            var hduData = Encoding.ASCII.GetString(headerData, 0, FitsConstants.HduHeaderRecordSize);
            var hdu = new HduHeaderRecord(hduData);
            if (hdu.Keyword != "SIMPLE")
                throw new Exception("FITS header should start with SIMPLE, invalid header");

            if (hdu.ValueAsString != "T")
                throw new Exception("Cannot process files that do not conform to FITS standard (expected SIMPLE = T)");
        }

        private void PopulateDimensions(FitsImage fitsImage)
        {
            fitsImage.Metadata.ImageWidth = fitsImage.HduHeaderRecords.First(x => x.Keyword == "NAXIS1").ValueAsInt;
            fitsImage.Metadata.ImageHeight = fitsImage.HduHeaderRecords.First(x => x.Keyword == "NAXIS2").ValueAsInt;
            fitsImage.Metadata.BitsPerPixel = fitsImage.HduHeaderRecords.First(x => x.Keyword == "BITPIX").ValueAsInt;

            SetCenterPos(fitsImage);
            SetViewSize(fitsImage);
        }

        private void SetCenterPos(FitsImage fitsImage)
        {

            var hdus = fitsImage.HduHeaderRecords;

            // The following FITS keywords should contain the RA/Dec coords:
            // OBJCTRA OBJCTDEC   - '13 29 46.26'  / Object J2000 RA in Hours, '47 10 58.94' / Object J2000 DEC in Degrees
            // RA DEC             - Object J2000 RA in Degrees, Object J2000 DEC in Degrees
            // RA_OBJ DEC_OBJ     - Object J2000 RA in Degrees, Object J2000 DEC in Degrees
            //
            // CRPIX1 CRPIX2 (double) (coordinate system reference pixel)
            // CRVAL1 CRVAL2 (double) (coordinate system value at reference pixel)
            // CRTYPE1 CRTYPE2 (string) (name of the coordinate axis) RA---TAN DEC--TAN
            // RADECSYS (string) FK5
            // EQUINOX (number) 2000

            // For now, just cover the three first cases, not sure if working with CR* is
            // relevant here since it kind of suggests the solving is already done.

            var ra = hdus.FirstOrDefault(x => x.Keyword == "RA");
            var dec = hdus.FirstOrDefault(x => x.Keyword == "DEC");

            if (ra != null && dec != null)
            {
                fitsImage.Metadata.CenterPos = GetCoordsFromRaDec(ra, dec);
                return;
            }

            var ra_obj = hdus.FirstOrDefault(x => x.Keyword == "RA_OBJ");
            var dec_obj = hdus.FirstOrDefault(x => x.Keyword == "DEC_OBJ");

            if (ra_obj != null && dec_obj != null)
            {
                fitsImage.Metadata.CenterPos = GetCoordsFromRaDec(ra_obj, dec_obj);
                return;
            }

            var objctra = hdus.FirstOrDefault(x => x.Keyword == "OBJCTRA");
            var objctdec = hdus.FirstOrDefault(x => x.Keyword == "OBJCTDEC");

            if (objctra != null && objctdec != null)
            {
                fitsImage.Metadata.CenterPos = GetCoordsFromObjctRaDec(objctra, objctdec);
                return;
            }

            fitsImage.Metadata.CenterPos = null;
        }

        private EquatorialCoords GetCoordsFromObjctRaDec(HduHeaderRecord ra, HduHeaderRecord dec)
        {
            var c = CultureInfo.InvariantCulture;
            var raHoursValue = ra.ValueAsString; // HH MM ss
            var decDegsValue = dec.ValueAsString; // DG MM ss

            var raElems = raHoursValue.Trim('\'', '\"', ' ').Split(' '); // Trim the quotes that some programs like to put in
            var decElems = decDegsValue.Trim('\'', '\"', ' ').Split(' ');

            // Because we may have '-00 xx xx'
            bool negativeSign = decElems[0].StartsWith("-"); // Used below
            decElems[0] = decElems[0].Replace("-", string.Empty);

            var raH = double.Parse(raElems[0], c);
            var raM = double.Parse(raElems[1], c);
            var raS = double.Parse(raElems[2], c);
            var raInDegrees = Conversions.RaToDecimal(raH, raM, raS);

            var decD = double.Parse(decElems[0], c);
            var decM = double.Parse(decElems[1], c);
            var decS = double.Parse(decElems[2], c);
            var decInDegrees = Conversions.DecToDecimal(negativeSign, decD, decM, decS);

            return new EquatorialCoords(raInDegrees, decInDegrees);
        }

        private EquatorialCoords GetCoordsFromRaDec(HduHeaderRecord ra, HduHeaderRecord dec)
        {
            return new EquatorialCoords(ra.ValueAsDouble, dec.ValueAsDouble);
        }

        private void SetViewSize(FitsImage fitsImage)
        {
            // To get the size, we need to know the focal length and the pixel size
            // and of course the image dimensions.

            var hdus = fitsImage.HduHeaderRecords;

            var pixSize1Um = hdus.FirstOrDefault(x => x.Keyword == "PIXSIZE1")?.ValueAsDouble;
            var pixSize2Um = hdus.FirstOrDefault(x => x.Keyword == "PIXSIZE2")?.ValueAsDouble;

            var xBinning = hdus.FirstOrDefault(x => x.Keyword == "XBINNING")?.ValueAsDouble ?? 1;
            var yBinning = hdus.FirstOrDefault(x => x.Keyword == "YBINNING")?.ValueAsDouble ?? 1;

            var focalLenMm = hdus.FirstOrDefault(x => x.Keyword == "FOCALLEN")?.ValueAsDouble;

            // Unable to calculate without these.
            if (focalLenMm == null || pixSize1Um == null || pixSize2Um == null)
            {
                fitsImage.Metadata.ViewSize = null;
                return;
            }


            var chipSizeXmm = pixSize1Um * xBinning * fitsImage.Metadata.ImageWidth / 1000.0;
            var chipSizeYmm = pixSize2Um * yBinning * fitsImage.Metadata.ImageHeight / 1000.0;

            var widthDeg = 2 * Math.Atan(chipSizeXmm.Value / (2 * focalLenMm.Value)) * (180.0 / Math.PI);
            var heightDeg = 2 * Math.Atan(chipSizeYmm.Value / (2 * focalLenMm.Value)) * (180.0 / Math.PI);

            fitsImage.Metadata.ViewSize = new ViewArea()
            {
                HeightDeg = heightDeg,
                WidthDeg = widthDeg
            };
        }

        private void ThrowIfUnsupportedFormat(FitsImage fitsImage)
        {
            if (fitsImage.HduHeaderRecords.First(x => x.Keyword == "NAXIS").ValueAsInt != 2)
                throw new Exception("Can only process FITS files with two axis (NAXIS = 2)");

            if (fitsImage.Metadata.BitsPerPixel != 8 && fitsImage.Metadata.BitsPerPixel != 16 && fitsImage.Metadata.BitsPerPixel != 32)
                throw new Exception("Floating point data is not supported. BITPIX values of 8, 16, and 32 are supported");
        }
    }
}