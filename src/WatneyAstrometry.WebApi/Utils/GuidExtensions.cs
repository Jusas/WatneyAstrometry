// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;

namespace WatneyAstrometry.WebApi.Utils
{
    public static class GuidExtensions
    {
        public static string Shortened(this Guid guid)
        {
            return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
        }
    }
}
