﻿// Copyright (c) Jussi Saarivirta.
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
        /// In which order do we want to search RA.
        /// </summary>
        public enum RaSearchOrder
        {
            /// <summary>
            /// Within Northern or Southern half, start search from the East side, and once East has
            /// been fully searched, switch to West side.
            /// </summary>
            EastFirst,
            /// <summary>
            /// Within Northern or Southern half, start search from the West side, and once West has
            /// been fully searched, switch to East side.
            /// </summary>
            WestFirst
        }

        /// <summary>
        /// In which order do we want to search Dec.
        /// </summary>
        public enum DecSearchOrder
        {
            /// <summary>
            /// Search 0..90 degrees declination first, searching RA in <see cref="RaSearchOrder"/>.
            /// </summary>
            NorthFirst,
            /// <summary>
            /// Search -90..0 degrees declination first, searching RA in <see cref="RaSearchOrder"/>.
            /// </summary>
            SouthFirst
        }

        public class Options
        {
            private float _startRadiusDegrees = 22.5f;

            /// <summary>
            /// This is the starting search area radius in degrees, i.e. the full sky
            /// will get searched in circles of this radius. If matches are not found,
            /// the search then continues on by halving the radius over and over,
            /// until the search completes or is stopped.
            /// </summary>
            public float StartRadiusDegrees
            {
                get => _startRadiusDegrees;
                set
                {
                    if (value > 30 || value <= 0)
                        throw new Exception("Start radius should be > 0 and <= 30 degrees");
                    _startRadiusDegrees = value;
                }
            }

            private float _minRadiusDegrees = 22.5f / 32;

            /// <summary>
            /// Minimum radius for the search.
            /// </summary>
            public float MinRadiusDegrees
            {
                get => _minRadiusDegrees;
                set
                {
                    if (value <= 0 || value > 30)
                        throw new Exception("Minimum radius must be > 0 and <= 30");
                    _minRadiusDegrees = value;
                }
            }

            /// <summary>
            /// Which side of the sky sphere to start with.
            /// </summary>
            public RaSearchOrder SearchOrderRa { get; set; } = RaSearchOrder.EastFirst;
            /// <summary>
            /// Which pole of the sky sphere to start with.
            /// </summary>
            public DecSearchOrder SearchOrderDec { get; set; } = DecSearchOrder.NorthFirst;
            
            /// <summary>
            /// Controls the inclusion of lower density quad passes. <br/>
            /// Including more passes in search increases the chance of detection, but also increases solve time.<br/>
            /// Example: Setting this to 2 will include two lower quad density passes in our search (if available in the quad database).
            /// <br/>Defaults to 0.
            /// </summary>
            public uint MaxNegativeDensityOffset { get; set; }
            
            /// <summary>
            /// Controls the inclusion of higher density quad passes. <br/>
            /// Including more passes in search increases the chance of detection, but also increases solve time.<br/>
            /// Example: Setting this to 2 will include two higher quad density passes in our search (if available in the quad database).
            /// <br/>Defaults to 0.
            /// </summary>
            public uint MaxPositiveDensityOffset { get; set; }

            /// <summary>
            /// Allow the use of parallelism, searching multiple areas simultaneously. <br/>
            /// This may use more memory, but is significantly faster. <br/>
            /// Defaults to true.
            /// </summary>
            public bool UseParallelism { get; set; } = true;
        }

        public Options StrategyOptions { get; internal set; }
        public int[] DensityOffsets { get; internal set; }

        public BlindSearchStrategy()
        {
        }

        public BlindSearchStrategy(Options options = null)
        {
            StrategyOptions = options ?? new Options();
            UseParallelism = StrategyOptions.UseParallelism;
            if (StrategyOptions.MinRadiusDegrees > StrategyOptions.StartRadiusDegrees)
                throw new Exception("MinRadiusDegrees must be <= StartRadiusDegrees");
            DensityOffsets = new int[StrategyOptions.MaxNegativeDensityOffset + StrategyOptions.MaxPositiveDensityOffset + 1];
            for (int i = -(int)StrategyOptions.MaxNegativeDensityOffset, n = 0; i <= StrategyOptions.MaxPositiveDensityOffset; i++, n++)
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
            var radius = StrategyOptions.StartRadiusDegrees;
            
            //- Rows are zigzagging. Spacing is always constant, until too much calculated overlap.
            //	- First row, dec 0, spacing = diameter
            //	- Second row, dec = radius, spacing = diameter, RA offset is radius
            //	- Third row, dec = radius * 2, spacing = diameter
            //	--> Overlap is increasing, the more dec increases/decreases.
            //	Once overlap is too much, remove one (or more) search circles, sum the removed circles diameters, and add that to spacing (more spacing per circle, removed_diameter_sum divided by new circle count-1)
            //	Basically when overlap reaches the size of one search circle,
            //	when Dec reaches a value where 180 - cos(dec) * 180 == diameter


            while (radius >= StrategyOptions.MinRadiusDegrees)
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
                        if (StrategyOptions.SearchOrderDec == DecSearchOrder.SouthFirst && decIteration < 2)
                            actualDec = -dec;
                        else if (StrategyOptions.SearchOrderDec == DecSearchOrder.NorthFirst && decIteration >= 2)
                            actualDec = -dec;

                        for (var i = 0; i < numberOfSearchCircles; i++)
                        {
                            var ra = (raOffset + i * raStep) % 180.0f; // is this % necessary?
                            var actualRa = ra;

                            if (StrategyOptions.SearchOrderRa == RaSearchOrder.WestFirst && decIteration % 2 == 0)
                                actualRa += 180;
                            else if (StrategyOptions.SearchOrderRa == RaSearchOrder.EastFirst && decIteration % 2 == 1)
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