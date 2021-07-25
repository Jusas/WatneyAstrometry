using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VizUtils;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class BandAndCellsTests
    {

        public class CellBoundsTestData : IEnumerable<object[]>
        {

            public IEnumerator<object[]> GetEnumerator()
            {
                // <radius>, <center>, <bounds>, <should be inside or not>
                yield return new object[] { 8.0, new EquatorialCoords(355, -25), SkySegmentSphere.GetCellById("b10c00").Bounds, true };
                yield return new object[] { 15.0, new EquatorialCoords(250, 20), SkySegmentSphere.GetCellById("b08c25").Bounds, true };
                yield return new object[] { 10.0, new EquatorialCoords(50, 30), SkySegmentSphere.GetCellById("b02c01").Bounds, false };
                yield return new object[] { 8.0, new EquatorialCoords(0, 0), SkySegmentSphere.GetCellById("b09c35").Bounds, true };
                yield return new object[] { 15.0, new EquatorialCoords(125, 80), SkySegmentSphere.GetCellById("b01c04").Bounds, true };
            }


            public static (double radius, EquatorialCoords center, RaDecBounds bounds, bool inside) ParseObject(object[] o)
                => ((double) o[0], (EquatorialCoords) o[1], (RaDecBounds) o[2], (bool) o[3]);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }


        [Theory]
        [ClassData(typeof(CellBoundsTestData))]
        public void Should_find_cells_in_search_radius(double radius, EquatorialCoords center, RaDecBounds bounds, bool inside)
        {
            BandsAndCells.IsCellInSearchRadius(radius, center, bounds).Should().Be(inside);
        }

        [Fact]
        [Trait("Category", "Visual")]
        public void Draw_visual_cell_test_representation()
        {
            
            var candidateSegments = new CellBoundsTestData();
            
            using (var image = SkySegmentViz.DrawSkySegmentSphere(2))
            {
                
                foreach (var candidate in candidateSegments)
                {
                    var c = CellBoundsTestData.ParseObject(candidate);
                    var isInside = BandsAndCells.IsCellInSearchRadius(c.radius, c.center, c.bounds);

                    // As we go up/down, one degree covers more RA degrees. Need to distort the circle to reflect that on flat 2D plane.
                    var distortion = 1.0 / Math.Cos(Conversions.Deg2Rad(c.center.Dec));

                    var randomR = (byte)new Random().Next(40, 255);
                    var randomG = (byte)new Random().Next(40, 255);
                    var randomB = (byte)new Random().Next(40, 255);

                    var ellipseColor = new Argb32(randomR, randomG, randomB, 225);
                    var cellColor = new Argb32(randomR, randomG, randomB, isInside ? (byte)150 : (byte)50);
                    var outlineColor = new Argb32(255, 0, 0, 200);
                    
                    image.DrawColoredEllipseOnSkySegments(c.center, distortion * c.radius, c.radius, ellipseColor);
                    image.DrawColoredSkySegmentCell(c.bounds, cellColor);
                    if (isInside)
                        image.DrawColoredSkySegmentCellOutline(c.bounds, outlineColor);
                }
                
                image.SaveAsPng($"{nameof(Draw_visual_cell_test_representation)}.png");
            }

        }

        [Fact]
        [Trait("Category", "Visual")]
        public void Draw_cells_in_range()
        {

            var testData = new CellBoundsTestData();

            using (var image = SkySegmentViz.DrawSkySegmentSphere(2))
            {

                foreach (var item in testData)
                {
                    var c = CellBoundsTestData.ParseObject(item);
                    var randomR = (byte)new Random().Next(40, 255);
                    var randomG = (byte)new Random().Next(40, 255);
                    var randomB = (byte)new Random().Next(40, 255);

                    // As we go up/down, one degree covers more RA degrees. Need to distort the circle to reflect that on flat 2D plane.
                    var distortion = 1.0 / Math.Cos(Conversions.Deg2Rad(c.center.Dec));

                    var ellipseColor = new Argb32(randomR, randomG, randomB, 225);
                    var cellColor = new Argb32(randomR, randomG, randomB, (byte)128);
                    var outlineColor = new Argb32(255, 0, 0, 200);

                    image.DrawColoredEllipseOnSkySegments(c.center, distortion * c.radius, c.radius, ellipseColor);

                    foreach (var cell in SkySegmentSphere.Cells)
                    {
                        if (BandsAndCells.IsCellInSearchRadius(c.radius, c.center, cell.Bounds))
                        {
                            image.DrawColoredSkySegmentCell(cell.Bounds, cellColor);
                            image.DrawColoredSkySegmentCellOutline(cell.Bounds, outlineColor);
                        }
                    }
                    
                }

                image.SaveAsPng($"{nameof(Draw_cells_in_range)}.png");
            }

        }

    }
}