using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    class Missile : IDirectedVisual {
        internal int Lifespan { get; set; }

        readonly double dXStep, dYStep;
        internal string Id { get; private set; }

        internal Missile(string missileId, int remainingLife, double x, double y, double heading) {
            Position = new Position(x, y);
            Heading = heading;
            Lifespan = remainingLife;
            Id = missileId;
            double distPerStep = WorldConstants.MissileSpeed / WorldConstants.StepsPerTurn;
            dXStep = distPerStep * Math.Sin(Heading.ToRadians());
            dYStep = -distPerStep * Math.Cos(Heading.ToRadians());
        }

        internal void Move() {
            Position.X += dXStep;
            Position.Y += dYStep;
        }

        public string Texture {
            get { return "missile"; }
        }

        public Position Position { get; set; }
        public double Radius { get { return WorldConstants.MissileRadius; } }
        public double Heading { get; private set; }
    }
}
