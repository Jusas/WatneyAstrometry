using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.SolverApp
{
    public class XyList : IImageDimensions
    {
        public static XyList FromStream(Stream stream)
        {

            var xyList = new XyList();

            // A simple and dumb check that the file is a FITS and contains the headers
            // we require, and that the first extension is BINTABLE and contains
            // 2 axis with 3 fields per row.

            // Expecting to read at least 2 HDUs, the first one with no data.
            // The second one is XTENSION='BINTABLE' and should contain NAXIS=2 data.
            // If we don't get all this, we can't use it.

            if (stream.Length <= FitsConstants.HeaderBlockSize * 2)
            {
                return null;
            }
            
            var hduBytes = new byte[2880];
            var end = false;
            var records = new List<(string name, string value)>();

            stream.Read(hduBytes, 0, hduBytes.Length);
            var simple = Encoding.ASCII.GetString(hduBytes, 0, 8);
            if (simple != "SIMPLE  ")
                return null;
            
            // Read HDUs until we reach the first END keyword.
            do
            {
                for (var i = 0; i < FitsConstants.HeaderBlockSize / FitsConstants.HduHeaderRecordSize; i++)
                {
                    var record = Encoding.ASCII.GetString(hduBytes,
                        i * FitsConstants.HduHeaderRecordSize, FitsConstants.HduHeaderRecordSize);
                    var (keyword, value) = GetKeyValue(record);
                    if (keyword == "END")
                    {
                        end = true;
                        break;
                    }
                    
                    if (keyword == "NAXIS" && value != "0")
                        return null;

                    if (keyword == "EXTEND" && value != "T")
                        return null;

                    records.Add((keyword, value));
                }
            } while (!end && (stream.Read(hduBytes, 0, hduBytes.Length)) == FitsConstants.HeaderBlockSize);


            // If there's an extension, we have more HDUs.

            var bytesRead = stream.Read(hduBytes, 0, hduBytes.Length);
            if (bytesRead != FitsConstants.HeaderBlockSize)
                return null;

            var xtension = Encoding.ASCII.GetString(hduBytes, 0, 8);
            if (xtension != "XTENSION")
                return null;

            records.Clear();
            end = false;

            // Read HDUs until we reach the first END keyword.
            do
            {
                for (var i = 0; i < FitsConstants.HeaderBlockSize / FitsConstants.HduHeaderRecordSize; i++)
                {
                    var record = Encoding.ASCII.GetString(hduBytes,
                        i * FitsConstants.HduHeaderRecordSize, FitsConstants.HduHeaderRecordSize);
                    var (keyword, value) = GetKeyValue(record);
                    if (keyword == "END")
                    {
                        end = true;
                        break;
                    }

                    if (keyword == "NAXIS" && value != "2")
                        return null;

                    if (keyword == "XTENSION" && value != "BINTABLE")
                        return null;

                    records.Add((keyword, value));
                }
            } while (!end && (stream.Read(hduBytes, 0, hduBytes.Length)) == FitsConstants.HeaderBlockSize);


            // We expect data with 3 fields in each row.
            var fields = records.FirstOrDefault(x => x.name == "TFIELDS");
            if (fields == default || fields.value != "3")
                return null;
            
            var colWidth = records.FirstOrDefault(x => x.name == "NAXIS1");
            if (colWidth == default)
                return null;

            var rows = records.FirstOrDefault(x => x.name == "NAXIS2");
            if (rows == default)
                return null;
            
            // Expect 'E' (float) for all
            if (records.Where(x => x.name.StartsWith("TFORM")).Any(x => x.value != "E" && x.value != "1E"))
                return null;

            var rowsInt = int.Parse(rows.value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var floatBuf = new byte[4];
            var floatBuf2 = new byte[4];

            float ReadFloat()
            {
                stream.Read(floatBuf, 0, 4);
                if (BitConverter.IsLittleEndian)
                {
                    floatBuf2[0] = floatBuf[3];
                    floatBuf2[1] = floatBuf[2];
                    floatBuf2[2] = floatBuf[1];
                    floatBuf2[3] = floatBuf[0];
                    return BitConverter.ToSingle(floatBuf2);
                }
                return BitConverter.ToSingle(floatBuf);
            }


            for (var r = 0; r < rowsInt; r++)
            {
                var x = ReadFloat();
                var y = ReadFloat();
                // These are magnitudes, smaller is brighter.
                // The solver works with pixel values, higher value is brighter, so basically inverse these.
                // The numbers themselves don't matter, the order does.
                var mag = 1_000_000 - ReadFloat() * 10_000;

                xyList.Stars.Add(new ImageStar(x, y, (long)mag, 1));
            }

            var brights = xyList.Stars.Select(x => x.Brightness).Distinct().ToArray();
            return xyList;

        }

        private static (string name, string value) GetKeyValue(string record)
        {
            var keyword = record.Substring(0, 8).Trim();
            var value = record.Substring(9);
            if (value.Contains("/"))
                value = value.Substring(0, value.IndexOf('/')).Trim().Trim('\'').Trim();
            else
                value = value.Trim().Trim('\'').Trim();
            return (keyword, value);
        }
        

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public List<ImageStar> Stars { get; } = new();
    }
}