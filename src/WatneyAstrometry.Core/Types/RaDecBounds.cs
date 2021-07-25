// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    public class RaDecBounds
    {
        public double RaLeft { get; set; }
        public double RaRight { get; set; }
        public double DecTop { get; set; }
        public double DecBottom { get; set; }

        private EquatorialCoords _center;

        public EquatorialCoords Center
        {
            get
            {
                if (_center == null)
                    _center = new EquatorialCoords(RaLeft + (RaRight - RaLeft) / 2,
                        DecBottom + (DecTop - DecBottom) / 2);
                return _center;
            }
        }

        public RaDecBounds()
        {
            
        }

        public RaDecBounds(double raLeft, double raRight, double decTop, double decBottom)
        {
            RaLeft = raLeft;
            RaRight = raRight;
            DecTop = decTop;
            DecBottom = decBottom;
        }

        public bool IsInside(double ra, double dec) =>
            ra >= RaLeft && ra <= RaRight && dec >= DecBottom && dec <= DecTop;

        public bool IsInside(EquatorialCoords coords) =>
            IsInside(coords.Ra, coords.Dec);
    }
}