// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A search strategy for performing blind solves. In blind solves we don't have any
    /// data available from the image, instead we just try different field sizes and scan
    /// the sky.
    /// </summary>
    public class BlindSearchStrategy : ISearchStrategy
    {
        /// <summary>
        /// The search options.
        /// </summary>
        public BlindSearchStrategyOptions Options { get; internal set; }
        /// <summary>
        /// Density offsets, created from <see cref="BlindSearchStrategyOptions.MaxNegativeDensityOffset"/> and <see cref="BlindSearchStrategyOptions.MaxPositiveDensityOffset"/>.
        /// </summary>
        public int[] DensityOffsets { get; internal set; }

        /// <summary>
        /// Empty constructor; for deserialization purposes.
        /// </summary>
        public BlindSearchStrategy()
        {
        }

        /// <summary>
        /// Create a new blind search strategy with the specified options.
        /// </summary>
        /// <param name="options">The search options.</param>
        public BlindSearchStrategy(BlindSearchStrategyOptions options = null)
        {
            Options = options ?? new BlindSearchStrategyOptions();
            UseParallelism = Options.UseParallelism;
            if (Options.MinRadiusDegrees > Options.StartRadiusDegrees)
                throw new Exception("MinRadiusDegrees must be <= StartRadiusDegrees");
            DensityOffsets = new int[Options.MaxNegativeDensityOffset + Options.MaxPositiveDensityOffset + 1];
            for (int i = -(int)Options.MaxNegativeDensityOffset, n = 0; i <= Options.MaxPositiveDensityOffset; i++, n++)
                DensityOffsets[n] = i;
        }

        /// <summary>
        /// Slices the <see cref="BlindSearchStrategy"/> into multiple <see cref="PartialBlindSearchStrategy"/> instances, slicing the sky sphere by RA coordinate.
        /// </summary>
        /// <returns></returns>
        public List<PartialBlindSearchStrategy> Slice(int slices)
        {
            if (slices < 2)
                throw new SolverException("Must have at least 2 slices, otherwise splitting doesn't make sense.");

            var sliceStrategies = new List<PartialBlindSearchStrategy>(
                Enumerable.Range(0, slices).Select(i => new PartialBlindSearchStrategy()
                {
                    UseParallelism = UseParallelism
                }));

            // Collect the SearchRuns into RA slices
            var raSliceWidth = 360.0f / slices;
            foreach (var searchRun in GetSearchQueue())
            {
                for (var i = 0; i < slices; i++)
                {
                    var sectorStart = i * raSliceWidth;
                    var sectorEnd = (i + 1) * raSliceWidth;
                    if (searchRun.Center.Ra >= sectorStart && searchRun.Center.Ra <= sectorEnd)
                    {
                        sliceStrategies[i].SearchRuns.Add(searchRun);
                        break;
                    }
                }
            }

            return sliceStrategies;

        }

        /// <inheritdoc />
        public IEnumerable<SearchRun> GetSearchQueue()
        {
            var radius = Options.StartRadiusDegrees;
            
            //- Rows are zigzagging. Spacing is always constant, until too much calculated overlap.
            //	- First row, dec 0, spacing = diameter
            //	- Second row, dec = radius, spacing = diameter, RA offset is radius
            //	- Third row, dec = radius * 2, spacing = diameter
            //	--> Overlap is increasing, the more dec increases/decreases.
            //	Once overlap is too much, remove one (or more) search circles, sum the removed circles diameters, and add that to spacing (more spacing per circle, removed_diameter_sum divided by new circle count-1)
            //	Basically when overlap reaches the size of one search circle,
            //	when Dec reaches a value where 180 - cos(dec) * 180 == diameter


            while (radius >= Options.MinRadiusDegrees)
            {
                // 4 iterations: positive and negative on east side, positive and negative on west side.
                for (var decIteration = 0; decIteration < 4; decIteration++)
                {
                    int n = 0;
                    //for (float dec = 0; dec <= 90; dec += radius, n++)
                    float dec = 0;
                    bool complete = false;
                    while(!complete)
                    {
                        if (dec >= 90)
                        {
                            dec = 90;
                            complete = true;
                        }

                        var angularDistToCover = Math.Cos(Conversions.Deg2Rad(dec)) * 180.0;
                        var numberOfSearchCircles = (int)Math.Ceiling(angularDistToCover / (2 * radius)) + 1;

                        var raStep = 180.0f / numberOfSearchCircles;
                        var raOffset = n % 2 * 0.5f * raStep;

                        // Adjust dec sign depending on which iteration we're on and what search ordering preference was used.
                        var actualDec = dec;
                        if (Options.SearchOrderDec == BlindSearchStrategyOptions.DecSearchOrder.SouthFirst && decIteration < 2)
                            actualDec = -dec;
                        else if (Options.SearchOrderDec == BlindSearchStrategyOptions.DecSearchOrder.NorthFirst && decIteration >= 2)
                            actualDec = -dec;

                        for (var i = 0; i < numberOfSearchCircles; i++)
                        {
                            var ra = (raOffset + i * raStep) % 180.0f; // is this % necessary?
                            var actualRa = ra;

                            if (Options.SearchOrderRa == BlindSearchStrategyOptions.RaSearchOrder.WestFirst && decIteration % 2 == 0)
                                actualRa += 180;
                            else if (Options.SearchOrderRa == BlindSearchStrategyOptions.RaSearchOrder.EastFirst && decIteration % 2 == 1)
                                actualRa += 180;

                            yield return new SearchRun()
                            {
                                Center = new EquatorialCoords(actualRa, actualDec),
                                RadiusDegrees = radius,
                                DensityOffsets = DensityOffsets
                            };
                        }

                        dec += radius;
                        n++;
                    }
                    
                }

                radius /= 2;
            }


           
        }

        /// <inheritdoc />
        public bool UseParallelism { get; set; }
    }
}