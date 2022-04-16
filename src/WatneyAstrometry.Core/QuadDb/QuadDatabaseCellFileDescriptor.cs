// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using System.Text;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    internal class QuadDatabaseCellFileDescriptor
    {

        public class SubCellInfo
        {
            public EquatorialCoords Center { get; set; }
            public int DataLengthBytes { get; set; }
            public long DataStartPos { get; set; }
        }

        public class Pass
        {
            public float QuadsPerSqDeg { get; set; }
            public int SubDivisions { get; set; }
            public SubCellInfo[] SubCells { get; set; }
            public double AvgSubCellRadius { get; set; }
            public int DataBlockByteLength { get; set; }
        }
        
        public string CellId { get; set; }
        public Pass[] Passes { get; set; }
        public string Filename { get; private set; }

        public bool BytesNeedReversing { get; private set; }
        

        private QuadDatabaseCellFileDescriptor()
        {
        }

        public static QuadDatabaseCellFileDescriptor FromIndexStream(Stream indexDataStream, string directory, bool dataIsLittleEndian)
        {
            var descriptor = new QuadDatabaseCellFileDescriptor();
            descriptor.BytesNeedReversing = BitConverter.IsLittleEndian != dataIsLittleEndian;
            descriptor.ReadFromStream(indexDataStream);
            descriptor.Filename = Path.Combine(directory, descriptor.Filename);
            return descriptor;
        }


        private void ReadFromStream(Stream stream)
        {

            var buf = new byte[255];
            stream.Read(buf, 0, 1);
            var filenameLen = buf[0];

            stream.Read(buf, 0, filenameLen);
            Filename = new UTF8Encoding(false).GetString(buf, 0, filenameLen);

            
            Cell cellReference;
        

            // Band and cell indices + pass count
            stream.Read(buf, 0, 12);

            if (BytesNeedReversing)
            {
                Array.Reverse(buf, 0, 4);
                Array.Reverse(buf, 4, 4);
                Array.Reverse(buf, 8, 4);
            }

            var band = BitConverter.ToInt32(buf, 0);
            var cell = BitConverter.ToInt32(buf, 4);
            var passCount = BitConverter.ToInt32(buf, 8);

            CellId = Cell.GetCellId(band, cell);
            //cellReference = SkySegmentSphere.GetCellById(CellId);
            cellReference = SkySegmentSphere.GetCellByBandAndCellIndex(band, cell);

            Passes = new Pass[passCount];

            // We don't have any human readable header in v3 format, just the identifier
            // + version number and data follows immediately after, as the index data
            // is now in the index file.
            var dataStartPos = "WATNEYQDB".Length + sizeof(int);

            for (var p = 0; p < passCount; p++)
            {
                var pass = new Pass();
                Passes[p] = pass;

                stream.Read(buf, 0, 12);

                if (BytesNeedReversing)
                {
                    Array.Reverse(buf, 0, 4);
                    Array.Reverse(buf, 4, 4);
                    Array.Reverse(buf, 8, 4);
                }

                pass.QuadsPerSqDeg = BitConverter.ToSingle(buf, 0);
                pass.SubDivisions = BitConverter.ToInt32(buf, 4);
                var numSubCells = BitConverter.ToInt32(buf, 8);
                pass.SubCells = new SubCellInfo[numSubCells];

                for (var sc = 0; sc < numSubCells; sc++)
                {
                    stream.Read(buf, 0, 12);

                    if (BytesNeedReversing)
                    {
                        Array.Reverse(buf, 0, 4);
                        Array.Reverse(buf, 4, 4);
                        Array.Reverse(buf, 8, 4);
                    }

                    var ra = BitConverter.ToSingle(buf, 0);
                    var dec = BitConverter.ToSingle(buf, 4);
                    var dataLength = BitConverter.ToInt32(buf, 8);

                    pass.SubCells[sc] = new SubCellInfo()
                    {
                        Center = new EquatorialCoords(ra, dec),
                        DataLengthBytes = dataLength
                    };
                    pass.DataBlockByteLength += dataLength;
                }
            }

            for (var p = 0; p < Passes.Length; p++)
            {
                var pass = Passes[p];
                for (var s = 0; s < pass.SubCells.Length; s++)
                {
                    pass.SubCells[s].DataStartPos = dataStartPos;
                    dataStartPos += pass.SubCells[s].DataLengthBytes;

                    if (pass.SubCells.Length == 1)
                        pass.AvgSubCellRadius = 0.5 * EquatorialCoords.GetAngularDistanceBetween(
                            new EquatorialCoords(cellReference.Bounds.RaLeft, cellReference.Bounds.DecTop),
                            new EquatorialCoords(cellReference.Bounds.RaRight, cellReference.Bounds.DecBottom));
                    else
                    {
                        var spanningDistance = EquatorialCoords.GetAngularDistanceBetween(
                            pass.SubCells[0].Center,
                            pass.SubCells[pass.SubCells.Length-1].Center);
                        pass.AvgSubCellRadius = spanningDistance / (pass.SubDivisions - 1) / 2;
                    }
                }
            }
            

        }
    }
}