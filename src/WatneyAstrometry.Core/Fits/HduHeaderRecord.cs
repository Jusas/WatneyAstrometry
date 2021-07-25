// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;

namespace WatneyAstrometry.Core.Fits
{
    internal class HduHeaderRecord
    {
        public string Keyword { get; set; }
        public string ValueAsString { get; set; }
        public string Comment { get; set; }

        public int ValueAsInt => int.Parse(ValueAsString, CultureInfo.InvariantCulture);
        public double ValueAsDouble => double.Parse(ValueAsString, CultureInfo.InvariantCulture);

        public HduHeaderRecord(string sourceData)
        {
            Populate(sourceData);
        }

        private void Populate(string sourceData)
        {
            Keyword = sourceData.Substring(0, 8).TrimEnd();
            
            var data = sourceData.Substring(Keyword == "COMMENT" ? 8 : 9).Trim();
            if (data.StartsWith("'"))
            {
                var stringEnd = data.IndexOf('\'', 1);
                ValueAsString = data.Substring(0, stringEnd+1);
                var commentStart = data.IndexOf('/', stringEnd);
                if (commentStart != -1)
                    Comment = data.Substring(commentStart);
            }
            else
            {
                var commentStart = data.IndexOf('/');
                if (commentStart != -1)
                    Comment = data.Substring(commentStart);

                ValueAsString = data.Substring(0, commentStart != -1 ? commentStart : data.Length).Trim();
            }
        }
    }
}