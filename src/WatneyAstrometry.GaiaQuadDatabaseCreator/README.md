# Gaia 2 Quad Database Creator

This program reads the produced outputs of the _Gaia 2 Star Extractor_ to create star quads.
The idea is to pre-calculate quads of the stars in each cell. This creates us a database to use to find matches between catalog star quads and image star quads.

## Parameters

TODO

## Data Format

The data is stored in a compact binary form in the generated 406 files/cells. The structure of each file is as follows:

```
WATNEYQDB<binary integer><header string as json><null character><binary data>
```

- All files are expected to start with the ascii characters `WATNEYQDB`.
- After that, a single integer (4 bytes) is written which is the file format version number. This is meant to be changed when the format structure changes.
- A human readable JSON header follows. The only purpose of this header is to give a brief description of the data to any curious users.
- A null character marks the end of the JSON header.
- The data begins.

Additionally the binary data is organized into passes and sub cells. The first block of data contains some pass and sub cell definitions, data lengths, etc. The quad data itself is stored in a compact format of 2 bytes per star distance ratio, plus quad center location (RA, Dec) and the largest distance as floats. See the source for more details.
