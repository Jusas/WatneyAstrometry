# Watney Astrometry Image Readers

This library contains `IImageReader` implementations for the **WatneyAstrometry.Core** library to use. The purpose of this library is to provide reader implementations for more image formats (currently png, jpeg). The library is kept separate to minimize the necessary dependencies of the core library.

These readers read different images in different pixel formats and convert them to monochrome 8/16/32 bit pixel buffers which the core solver can handle.