using System;
using System.Globalization;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.MathUtils
{
    /// <summary>
    /// Some basic, often used conversions.
    /// </summary>
    public static class Conversions
    {
        /// <summary>
        /// Degrees to radians.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static double Deg2Rad(double degrees) => Math.PI / 180 * degrees;

        /// <summary>
        /// Radians to degrees.
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static double Rad2Deg(double radians) => 180.0 / Math.PI * radians;

        /// <summary>
        /// RA from hours, minutes and seconds to single decimal number form.
        /// </summary>
        /// <param name="hh"></param>
        /// <param name="mm"></param>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static double RaToDecimal(int hh, int mm, int ss) =>
            RaToDecimal((double) hh, (double) mm, (double) ss);

        /// <summary>
        /// RA from hours, minutes and seconds to single decimal number form.
        /// </summary>
        /// <param name="hh"></param>
        /// <param name="mm"></param>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static double RaToDecimal(double hh, double mm, double ss) =>
            15.0 * (hh + mm / 60.0 + ss / 3600.0);

        /// <summary>
        /// RA from hours, minutes and seconds to single decimal number form. <br/>
        /// The valid string form is "hh mm ss.ss", e.g. "00 42 47.12"
        /// </summary>
        /// <param name="ra">String form of ra, e.g. "00 43 47.12"</param>
        /// <returns></returns>
        public static double RaToDecimal(string ra)
        {
            // Expecting "hh mm ss.ss"
            var numbers = ra.Split(' ');
            if (numbers.Length != 3)
                throw new Exception("ra: expected 3 numbers each separated by space");

            var parseOk = double.TryParse(numbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hh);
            if (!parseOk)
                throw new Exception("Could not parse hours");

            parseOk = double.TryParse(numbers[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mm);
            if (!parseOk)
                throw new Exception("Could not parse minutes");
            
            parseOk = double.TryParse(numbers[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ss);
            if (!parseOk)
                throw new Exception("Could not parse seconds");

            return RaToDecimal(hh, mm, ss);
        }

        /// <summary>
        /// Dec from degrees, minutes and seconds to single decimal number form.
        /// </summary>
        /// <param name="negative">True if degrees are positive but should be set negative</param>
        /// <param name="dd"></param>
        /// <param name="mm"></param>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static double DecToDecimal(bool negative, int dd, int mm, int ss) =>
            DecToDecimal(negative, (double)dd, (double)mm, (double)ss);

        /// <summary>
        /// Dec from degrees, minutes and seconds to single decimal number form.
        /// </summary>
        /// <param name="negative">True if degrees are positive but should be set negative</param>
        /// <param name="dd"></param>
        /// <param name="mm"></param>
        /// <param name="ss"></param>
        /// <returns></returns>
        public static double DecToDecimal(bool negative, double dd, double mm, double ss) =>
            (negative ? -1 : 1) * (dd + mm / 60.0 + ss / 3600.0);

        /// <summary>
        /// Dec from degrees, minutes and seconds to single decimal number form. <br/>
        /// The valid string form is "(+/-)dd mm ss.ss", e.g. "-76 06 13.12" or "41 16 8".
        /// </summary>
        /// <param name="dec">String form of dec, e.g. "-76 06 13.12" or "41 16 8".</param>
        /// <returns></returns>
        public static double DecToDecimal(string dec)
        {
            // Expecting "(+/-)dd mm ss.ss"
            var numbers = dec.Split(' ');
            if (numbers.Length != 3)
                throw new Exception("ra: expected 3 numbers each separated by space");

            var parseOk = double.TryParse(numbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dd);
            if (!parseOk)
                throw new Exception("Could not parse degrees");

            parseOk = double.TryParse(numbers[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mm);
            if (!parseOk)
                throw new Exception("Could not parse minutes");

            parseOk = double.TryParse(numbers[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ss);
            if (!parseOk)
                throw new Exception("Could not parse seconds");

            return DecToDecimal(dd < 0, Math.Abs(dd), mm, ss);
        }


        /// <summary>
        /// DateTime to Julian days.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static double DateTimeToJulianDays(DateTimeOffset dt)
        {
            var u = dt.ToUnixTimeSeconds();
            return u / 86400 + 2440587.5;
        }

        /// <summary>
        /// Dec from single decimal number to degrees, minutes and seconds format.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static string DecDegreesToDdMmSs(double degrees)
        {
            var negative = degrees < 0;
            degrees = Math.Abs(degrees);

            double mins = (degrees - (int)degrees) * 60;
            double ss = (mins - (int) mins) * 60;
            return $"{(negative ? "-" : "")}{(int)degrees} {(int) mins} {ss.ToString("0.##", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Ra from single decimal number to hours, minutes and seconds format.
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static string RaDegreesToHhMmSs(double degrees)
        {
            double hours = degrees / 15;
            double mins = (hours - (int)hours) * 60;
            double secs = (mins - (int) mins) * 60;

            return $"{(int) hours} {(int) mins} {secs.ToString("0.##", CultureInfo.InvariantCulture)}";

        }

        /// <summary>
        /// Degrees to hour angles, i.e. single numbers to degrees/hours, minutes and seconds strings.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static string[] DegreesToHourAngles(EquatorialCoords pt)
        {
            return new string[]
            {
                RaDegreesToHhMmSs(pt.Ra),
                DecDegreesToDdMmSs(pt.Dec)
            };
        }

        /// <summary>
        /// Julian day as an approximate epoch.
        /// </summary>
        /// <param name="julianDay"></param>
        /// <returns></returns>
        public static double JulianDayToApproximateEpoch(double julianDay)
        {
            double j2000JulianDays = 2451545.0;
            return Math.Round((julianDay - j2000JulianDays) / 365.25 + 2000, 1);
        }
    }
}