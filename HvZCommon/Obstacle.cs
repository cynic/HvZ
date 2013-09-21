using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public class Obstacle : ITakeSpace {
        public Position Position { get; private set; } // center of the object
        public double Radius { get; private set; }
        public string Texture { get { return "obstacle"; } }
        public Obstacle(double x, double y, double radius) {
            Position = new Position(x, y);
            Radius = radius;
        }
    }
}
