using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    internal class Obstacle : IVisual {
        public Position Position { get; private set; } // center of the object
        public double Radius { get; private set; }
        string IVisual.Texture { get { return "obstacle"; } }
        public Obstacle(double x, double y, double radius) {
            Position = new Position(x, y);
            Radius = radius;
        }
        public override string ToString() {
            return String.Format("Obstacle at {0}, radius {1}", Position, Radius);
        }
    }

    internal class SpawnPoint : IVisual {
        public Position Position { get; private set; } // center of the object
        public double Radius { get; private set; }
        string IVisual.Texture { get { return "spawnpoint"; } }
        public SpawnPoint(double x, double y, double radius) {
            Position = new Position(x, y);
            Radius = radius;
        }
        public override string ToString() {
            return String.Format("SpawnPoint/Obstacle at {0}, radius {1}", Position, Radius);
        }
    }
}
