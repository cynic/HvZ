using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    internal class Obstacle : IVisual {
        public Position Position { get; private set; } // center of the object
        public double Radius { get; private set; }
        public string Texture { get { return "obstacle"; } }
        public Obstacle(double x, double y, double radius) {
            Position = new Position(x, y);
            Radius = radius;
        }
    }

    internal class SpawnPoint : IVisual {
        public Position Position { get; private set; } // center of the object
        public double Radius { get; private set; }
        public string Texture { get { return "spawnpoint"; } }
        public SpawnPoint(double x, double y, double radius) {
            Position = new Position(x, y);
            Radius = radius;
        }
    }
}
