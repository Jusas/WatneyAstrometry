# Watney Astrometry Engine Core

The WatneyAstrometry.Core is the main code library that provides the actual solving algorithms. This library can be absorbed into any applications that want to use the solver.

The main parts of this library are:

- The quad database, and the algorithms to find quad matches between image-based quads and catalog star-based quads
- A light-weight FITS reader implementation, that can read monochrome FITS files
- Star detection algorithm to detect stars from a monochrome pixel buffer
- The main solver that runs the star detection, quad matching and finally calculates the astrometric solution based on the found matches
