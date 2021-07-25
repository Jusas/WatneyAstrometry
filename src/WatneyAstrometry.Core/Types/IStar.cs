// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    public interface IStar
    {
        double CalculateDistance(IStar anotherStar);
    }
}