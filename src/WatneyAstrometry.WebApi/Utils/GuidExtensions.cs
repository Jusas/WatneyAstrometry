// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text.RegularExpressions;

namespace WatneyAstrometry.WebApi.Utils
{
    /// <summary>
    /// Extensions for GUID struct.
    /// </summary>
    public static class GuidExtensions
    {
        /// <summary>
        /// Gets a slightly shortened version of the GUID, in base64 alphanumeric characters.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static string Shortened(this Guid guid)
        {
            return Regex.Replace(Convert.ToBase64String(guid.ToByteArray()), "[/+=]", "");
        }
    }
}
