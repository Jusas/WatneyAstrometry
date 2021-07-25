# Gaia 2 Star Extractor

This program reads the Gaia 2 source files (*.csv.gz), specifically reads the RA, Dec and Magnitude columns and stores them into the 406 files that represent the sky sphere cells. The sky sphere is split into these 406 roughly equal area cells (see https://arxiv.org/ftp/arxiv/papers/1612/1612.03467.pdf) and we gather the Gaia 2 stars into these cells for further processing into quads, taking only the relevant information that is needed for the solving process. This is done in order to make the generation of the actual quads that are used in recognising star patterns faster.

The logic is quite simple: the program reads the *.csv.gz files, it decompresses them on the fly, reads all stars up to the given magnitude and outputs them to whichever cell the stars belong to.
The output is plain binary files, and their only purpose is to be the star source for the _Gaia 2 Quad Database Creator_.
