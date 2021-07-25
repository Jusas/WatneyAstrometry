using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SimplePlateSolveLib.Image;
using SimplePlateSolveLib.MathUtils;
using SimplePlateSolveLib.StarDb;
using SimplePlateSolveLib.Types;
using VizUtils;
using Xunit;

namespace SimplePlateSolveLib.Tests
{
    public class StarDatabaseTests
    {

        private void CreateMockDatabase()
        {

        }

        [Fact]
        public async Task Should_retrieve_some_quads()
        {

            var starDb = new CompactQuadDatabase("gaia2");

            var cells = SkySegmentSphere.Cells;
            QuadDatabaseCellFileSet fileSetWithData = new QuadDatabaseCellFileSet(cells.First(c => c.CellId == "b08c11"), new [] {"Resources/gaia2-b08c11-0.00-13.00.sdb"});
            fileSetWithData.LoadIntoMemory();

            var emptyCache = new Dictionary<int, StarQuad[]>();
            Enumerable.Range(0, 256).ToList().ForEach(x => emptyCache.Add(x, new StarQuad[0]));

            var emptyCells = cells
                .Where(x => x.CellId != "b08c11")
                .Select(x => new QuadDatabaseCellFileSet(x) { _cachedQuads = emptyCache });

            starDb._cellFiles = emptyCells.Append(fileSetWithData).ToList();

            var center = new EquatorialCoords(116, 3);
            var centerHours = Conversions.DegreesToHourAngles(center);

            var foundStars = await starDb.GetQuadsAsync(center, 2, 50);
            var starCoordsTxt = foundStars.Select(x =>
                $"RA: {Conversions.RaDegreesToHhMmSs(x.MidPoint.Ra)}  Dec: {Conversions.DecDegreesToDdMmSs(x.MidPoint.Dec)}").ToList();
            foundStars.Should().NotBeEmpty();


        }

        [Fact]
        public async Task TestDrawImageAreaCatalogStars()
        {
            var starDb = new CompactQuadDatabase("tycho2");
            starDb.UseDataSource(@"Z:\tychodbj2k");

            DefaultFitsReader fitsReader = new DefaultFitsReader();
            var image = fitsReader.FromFile("Resources/m31.fits");
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_starsonly.png", true, false);
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_linesonly.png", false, true);

            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 1200, "m31_catalogstars_starsandquads__new.png", true, false);

            await starDb.VisualizeAreaAndFormQuads(new EquatorialCoords(194.464, 71.217), image.Metadata.ImageWidth, image.Metadata.ImageHeight,
                3.8, 1, 25, 500, "polar_area.png", true, false);


        }

        [Fact]
        public async Task TestDrawImageAreaCatalogStars2()
        {
            var starDb = new CompactQuadDatabase("tycho2");
            starDb.UseDataSource(@"Z:\tychodbj2k_2");

            DefaultFitsReader fitsReader = new DefaultFitsReader();
            var image = fitsReader.FromFile("Resources/ngc925.fits");
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_starsonly.png", true, false);
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_linesonly.png", false, true);

            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 1200, "m31_catalogstars_starsandquads__new.png", true, false);

            await starDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
                3.8, 1, 1480, 200, "ngc925_dbstars.png", true, true);


        }

        [Fact]
        public async Task TestDrawImageAreaCatalogStarsPole()
        {
            var starDb = new CompactQuadDatabase("tycho2");
            starDb.UseDataSource(@"Z:\tychodb");

            DefaultFitsReader fitsReader = new DefaultFitsReader();
            var image = fitsReader.FromFile("Resources/m31.fits");
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_starsonly.png", true, false);
            //await quadDb.VisualizeAreaAndFormQuads(image.Metadata.CenterPos, image.Metadata.ImageWidth, image.Metadata.ImageHeight,
            //    3.8, 1, 336, 800, "m31_catalogstars_linesonly.png", false, true);
            //await quadDb.VisualizeAreaAndFormQuads(new EquatorialCoords(10, 89.8), 4000, 3000,
            //    8, 1, 420, 1200, "pole_catalogstars_starsandquads.png", true, true);
            await starDb.VisualizeAreaAndFormQuads(new EquatorialCoords(10, 89.8), image.Metadata.ImageWidth, image.Metadata.ImageHeight,
                3.8, 1, 336, 1200, "pole_catalogstars_starsandquads__new.png", true, true);



        }
    }
    
}