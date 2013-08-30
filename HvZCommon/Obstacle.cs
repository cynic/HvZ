using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
    public class Obstacle : ITakeSpace {
        public Position Position { get; set; } // center of the object
        public double Radius { get; set; }
        public string Texture { get { return ""; } }
    }
}
