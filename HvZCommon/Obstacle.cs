using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon
{
    public abstract class Obstacle : ITakeSpace
    {
        public Position Position { get; set; } // center of the object
        public double Radius { get; set; }
        public abstract string TextureName { get; }
    }
}
