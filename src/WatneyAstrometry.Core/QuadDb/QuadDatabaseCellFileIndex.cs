// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    public class QuadDatabaseCellFileIndex
    {

        public bool DataIsLittleEndian { get; private set; }

        public QuadDatabaseCellFile[] CellFiles { get; private set; }

        private QuadDatabaseCellFileIndex(string filename, int fileSetId)
        {
            var directory = Path.GetDirectoryName(filename);
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read header.
                
                string expectedHeader = "WATNEYQDBINDEX";
                byte supportedVersion = 1;

                var buf = new byte[expectedHeader.Length];
                stream.Read(buf, 0, expectedHeader.Length);

                if (Encoding.ASCII.GetString(buf) != expectedHeader)
                    throw new Exception($"File {filename} is not a WATNEYQDBINDEX");

                stream.Read(buf, 0, 1);
                if (buf[0] != supportedVersion)
                {
                    throw new Exception($"This version of quad database index ({buf[0]}) is not supported " +
                        $"by this version of Watney. Supported versions: {supportedVersion}");
                }

                stream.Read(buf, 0, 1);
                var isLittleEndian = buf[0] == 1;

                DataIsLittleEndian = isLittleEndian;

                //var descriptors = new List<QuadDatabaseCellFileDescriptor>();
                var cellFiles = new List<QuadDatabaseCellFile>();

                var fileId = fileSetId * SkySegmentSphere.Cells.Count;

                while (stream.Position != stream.Length)
                {
                    var descriptor = QuadDatabaseCellFileDescriptor.FromIndexStream(stream, directory, DataIsLittleEndian);
                    var cellFile = new QuadDatabaseCellFile(descriptor, fileId);
                    cellFiles.Add(cellFile);
                    fileId++;
                }

                CellFiles = cellFiles.ToArray();
            }
        }

        public static QuadDatabaseCellFileIndex[] ReadAllIndexes(string directory)
        {
            var cellFileIndexFiles = Directory.GetFiles(directory, "*.qdbindex");

            var indexes = new QuadDatabaseCellFileIndex[cellFileIndexFiles.Length];

            Parallel.For(0, indexes.Length, (i) =>
            {
                var index = new QuadDatabaseCellFileIndex(cellFileIndexFiles[i], i);
                indexes[i] = index;
            });
            
            return indexes;
        }
        

    }
}