// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Text;

namespace WatneyAstrometry.GaiaQuadDatabaseCreator;

/// <summary>
/// An index file; holds the header data of multiple cell files.
/// Meant to be read once for the entire set of cell files.
/// Makes the database initialization step lighter on the solver
/// side since only one index file needs to be read instead of
/// reading the starting block of all 406 cell files.
/// </summary>
public class QuadDatabaseIndexFile
{
    public readonly object _lock = new object();

    private FileStream _fileStream;

    public QuadDatabaseIndexFile(string filename, bool resume)
    {

        if(File.Exists(filename) && resume)
            _fileStream = new FileStream(filename, FileMode.Append);
        else
        {
            _fileStream = new FileStream(filename, FileMode.Create);
            WriteHeader();
        }
        
    }

    private void WriteHeader()
    {
        // Header is simple; just used to recognize the file format.
        // string(WATNEYQDBINDEX) + byte(FORMATVERSION) + byte(IS_LITTLE_ENDIAN)

        string formatIdentifier = "WATNEYQDBINDEX";
        byte formatVersion = 1;

        // I think all devices that will run the solver are going to be little endian anyway,
        // but since there's still a remote possibility, let's just mark the byte order.

        byte isLittleEndian = BitConverter.IsLittleEndian ? (byte)1 : (byte)0;
        
        _fileStream.Write(Encoding.ASCII.GetBytes(formatIdentifier));
        _fileStream.WriteByte(formatVersion);
        _fileStream.WriteByte(isLittleEndian);
        
    }

    public void AppendCellToIndex(string cellFilename, Action<Stream> writer)
    {
        lock (_lock)
        {
            // Each cell file has an entry which begins with this:
            // byte(NUMBER_OF_CHARACTER_BYTES_IN_FILENAME) + string_utf8(CELL_FILENAME)

            var encoding = new UTF8Encoding(false);
            var filenameBytes = encoding.GetBytes(Path.GetFileName(cellFilename));

            _fileStream.WriteByte((byte)filenameBytes.Length);
            _fileStream.Write(filenameBytes);
            
            writer.Invoke(_fileStream);
            _fileStream.Flush();
        }
    }
}