// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.SolverVizTools.Models.Profile;

public static class ProfileToolTips
{
    public static readonly string TipBlindMinRadius = 
        "Minimum field radius when scanning the sky, in degrees.";

    public static readonly string TipBlindMaxRadius =
        "The starting (maximum) field radius in degrees. This field radius gets halved until minimum radius is reached, or a solution is found.";

    public static readonly string TipBlindSearchOrder =
        "Start the search from either the northern or southern half of the celestial sphere";

    public static readonly string TipNearbyInputSource =
        "Pick initial coordinates and field radius from FITS headers (the profile will work only on FITS images with these headers), or give them by manual input";

    public static readonly string TipNearbyRA =
        "The search center in RA coordinate (either decimal degrees  dd.dddd, e.g. 125.556  or in \"hh mm ss.ss\", e.g. \"17 05 23.5\").";

    public static readonly string TipNearbyDec =
        "The search center in Dec coordinate (either decimal degrees (+/-)dd.dddd, e.g. -45.667  or in \"(+/-)dd mm ss.ss\", e.g. \"-76 06 13.12\"; or \"41 16 8\")";

    public static readonly string TipNearbySearchRadius =
        "The radius around the center coordinate that will be searched";

    public static readonly string TipNearbyFieldRadiusSource =
        "Use a single field radius value to use in the search, or a min/max range with a number of intermediate steps in between.";

    public static readonly string TipNearbyFieldRadius =
        "Field radius to use in search";

    public static readonly string TipNearbyFieldRadiusMin =
        "Field radius range minimum value to use in search.";

    public static readonly string TipNearbyFieldRadiusMax =
        "Field radius range maximum value to use in search.";

    public static readonly string TipNearbyFieldRadiusSteps =
        "How many intermediate field radius values between min/max to use in search.";

    public static readonly string TipGeneralMaxStars =
        "Max number of detected stars to use in the search. The bigger the number, the more chances of finding a solution, but also the longer it may take. Recommended value is 300.";

    public static readonly string TipGeneralSampling =
        "Sampling subdivides the quad database into smaller sections that are searched serially, i.e trying to find a solution with a smaller number of comparisons. This can speed up blind searches significantly. Recommended values are 1 .. 16";

    public static readonly string TipGeneralHigherDensityOffset =
        "The quad database is searched on the basis of star density (stars per degree), and the database has quads grouped into \"passes\" by density. By increasing this number, further passes can be included into the search, improving the odds of finding a solution when image and quad database densities don't quite match. Recommended values are 1 .. 2";

    public static readonly string TipGeneralLowerDensityOffset =
        "The quad database is searched on the basis of star density (stars per degree), and the database has quads grouped into \"passes\" by density. By increasing this number, further passes can be included into the search, improving the odds of finding a solution when image and quad database densities don't quite match. Recommended values are 1 .. 2";







}