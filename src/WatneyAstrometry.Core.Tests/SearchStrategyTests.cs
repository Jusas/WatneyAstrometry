using System;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VizUtils;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;
using Xunit;

namespace WatneyAstrometry.Core.Tests
{
    public class SearchStrategyTests
    {
        // Here we have mainly visual aids, as they help to grasp what kind of areas
        // we are covering.

        [Fact]
        [Trait("Category", "Visual")]
        public void Visualize_blind_search_areas()
        {
            var options = new BlindSearchStrategy.Options()
            {
                SearchOrderRa = BlindSearchStrategy.RaSearchOrder.EastFirst,
                SearchOrderDec = BlindSearchStrategy.DecSearchOrder.NorthFirst,
                MinRadiusDegrees = 5,
                StartRadiusDegrees = 20
            };
            var strategy = new BlindSearchStrategy(options);
            var areas = strategy.GetSearchQueue().ToList();
            areas.Reverse();

            var colors = new Argb32[]
            {
                new Argb32(255, 0, 0, 50),
                new Argb32(100, 100, 0, 100),
                new Argb32(0, 100, 150, 150)
            };

            var prevRadius = areas.First().RadiusDegrees;
            var thickness = 1.0f;
            int colorIndex = 0;
            int imageIndex = 0;
            //foreach (var area in areas)
            Image<Rgba32> sky = SkySegmentViz.DrawSkySegmentSphere(4);
            for (var a = 0; a < areas.Count; a++)
            {
                var area = areas[a];
                if (prevRadius != area.RadiusDegrees)
                {
                    prevRadius = area.RadiusDegrees;
                    colorIndex++;
                    thickness += 1.0f;
                    sky.SaveAsPng($"{nameof(Visualize_blind_search_areas)}_{imageIndex}.png");
                    sky.Dispose();
                    imageIndex++;
                    sky = SkySegmentViz.DrawSkySegmentSphere(4);
                }

                // As we go up/down, one degree covers more RA degrees. Need to distort the circle to reflect that on flat 2D plane.
                // Since Cos(dec) can reach 0 here, insert some sanity.
                var distortion = Math.Min(100, 1.0 / Math.Cos(Conversions.Deg2Rad(area.Center.Dec)));
                sky.DrawColoredEllipseOutline(area.Center, area.RadiusDegrees * distortion, area.RadiusDegrees,
                    colors[colorIndex % colors.Length], thickness);

                if (a == areas.Count - 1)
                    sky.SaveAsPng($"{nameof(Visualize_blind_search_areas)}_{imageIndex}.png");
            }

        }

        [Fact]
        [Trait("Category", "Visual")]
        public void Visualize_nearby_search_areas()
        {
            var options = new NearbySearchStrategy.Options()
            {
                ScopeFieldRadius = 4,
                SearchAreaRadius = 20
            };
            var center = new EquatorialCoords(100, 30);

            var strategy = new NearbySearchStrategy(center, options);
            var areas = strategy.GetSearchQueue().ToList();

            using (var sky = SkySegmentViz.DrawSkySegmentSphere(4))
            {
                double distortion = 1;
                for (var a = 0; a < areas.Count; a++)
                {
                    var area = areas[a];
                    // As we go up/down, one degree covers more RA degrees. Need to distort the circle to reflect that on flat 2D plane.
                    // Since Cos(dec) can reach 0 here, insert some sanity.
                    distortion = Math.Min(100, 1.0 / Math.Cos(Conversions.Deg2Rad(area.Center.Dec)));
                    sky.DrawColoredEllipseOutline(area.Center, area.RadiusDegrees * distortion, area.RadiusDegrees,
                        Color.Red, 2.0f);
                }

                distortion = Math.Min(100, 1.0 / Math.Cos(Conversions.Deg2Rad(areas[0].Center.Dec)));
                sky.DrawColoredEllipseOutline(areas[0].Center, areas[0].RadiusDegrees * distortion,
                    areas[0].RadiusDegrees,
                    Color.Green, 2.0f);

                sky.SaveAsPng($"{nameof(Visualize_nearby_search_areas)}.png");
            }

        }
    }
}