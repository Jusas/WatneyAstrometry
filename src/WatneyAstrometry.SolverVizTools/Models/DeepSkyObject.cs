// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.SolverVizTools.Models
{
    public class DeepSkyObject
    {
        public enum DsoType
        {
            Galaxy,
            Nebula,
            PlanetaryNebula,
            OpenCluster,
            Other
        }
        
        public DsoType Type { get; set; }
        public EquatorialCoords Coords { get; set; }
        public double Angle { get; set; }
        public string Cat1 { get; set; }
        public string Id1 { get; set; }
        public string Cat2 { get; set; }
        public string Id2 { get; set; }
        public string Name { get; set; }
        public double Size1 { get; set; } // arcseconds
        public double Size2 { get; set; } // arcseconds

        public string DisplayName => !string.IsNullOrEmpty(Name)
            ? Name
            : !string.IsNullOrEmpty(Cat1) && !string.IsNullOrEmpty(Id1)
                ? $"{Cat1} {Id1}"
                : $"{Cat2} {Id2}";

        public static DeepSkyObject FromCsvLine(string csvLine)
        {
            //ra,dec,type,const,mag,name,rarad,decrad,id,r1,r2,angle,dso_source,id1,cat1,id2,cat2,dupid,dupcat,display_mag

            
            var fields = csvLine.Split(",");
            
            // ra is not in decimal degrees, need to convert it
            var ra = Double.Parse(fields[0], CultureInfo.InvariantCulture);
            ra = ra / 24 * 360;

            var obj = new DeepSkyObject()
            {
                Coords = new EquatorialCoords(ra, 
                    Double.Parse(fields[1], CultureInfo.InvariantCulture)),
                Type = GetObjectType(fields[2]),
                Name = fields[5],
                Angle = !string.IsNullOrEmpty(fields[11])
                    ? Double.Parse(fields[11], CultureInfo.InvariantCulture)
                    : 0,
                Id1 = fields[13],
                Cat1 = fields[14],
                Id2 = fields[15],
                Cat2 = fields[16],
                Size1 = !string.IsNullOrEmpty(fields[9]) 
                    ? Double.Parse(fields[9], CultureInfo.InvariantCulture) * 60.0
                    : 5,
                Size2 = !string.IsNullOrEmpty(fields[10]) 
                    ? Double.Parse(fields[10], CultureInfo.InvariantCulture) * 60.0
                    : 0
            };
            return obj;

        }

        public static DeepSkyObject ComparisonObject(double ra, double dec)
        {
            return new DeepSkyObject
            {
                Coords = new EquatorialCoords(ra, dec)
            };
        }

        private static DsoType GetObjectType(string typeStr)
        {
            switch (typeStr)
            {
                case "Gxy": return DsoType.Galaxy;
                case "Neb": return DsoType.Nebula;
                case "PN": return DsoType.PlanetaryNebula;
                case "OC": return DsoType.OpenCluster;
                default: return DsoType.Other;
            }
        }
    }
}
