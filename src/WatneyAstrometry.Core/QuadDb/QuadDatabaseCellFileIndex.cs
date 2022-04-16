// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Exceptions;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    internal class QuadDatabaseCellFileIndex
    {

        public bool DataIsLittleEndian { get; private set; }

        public QuadDatabaseCellFile[] CellFiles { get; private set; }

        public const string IndexFileFormatIdentifier = "WATNEYQDBINDEX";
        public const byte IndexFileFormatVersion = 1;

        private QuadDatabaseCellFileIndex(string filename, int fileSetId)
        {
            var directory = Path.GetDirectoryName(filename);
            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read header.
                
                var buf = new byte[IndexFileFormatIdentifier.Length];
                stream.Read(buf, 0, IndexFileFormatIdentifier.Length);

                if (Encoding.ASCII.GetString(buf) != IndexFileFormatIdentifier)
                    throw new QuadDatabaseVersionException($"File {filename} is not a {IndexFileFormatIdentifier}");

                stream.Read(buf, 0, 1);
                if (buf[0] != IndexFileFormatVersion)
                {
                    throw new QuadDatabaseVersionException($"This version of quad database index ({buf[0]}) is not supported " +
                        $"by this version of Watney. Supported versions: {IndexFileFormatVersion}");
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
            if (cellFileIndexFiles.Length == 0)
                throw new QuadDatabaseVersionException(
                    "No .qdbindex files were found. This version of Watney requires the separately indexed database files. Please download and use the correct database files.");

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