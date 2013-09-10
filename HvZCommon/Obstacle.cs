using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public class Obstacle : ITakeSpace {
        public Position Position { get; set; } // center of the object
        public double Radius { get; set; }
        public string Texture { get { return ""; } }
        public bool MustDespawn { get { return false; } }
    }

    public class ExplosionEffect : ITakeSpace {

        public Position Position { get; set; }
        public double Radius { get; set; }

        public DateTime CreationTime { get; private set; }
        public string Texture { get { return "explode"; } }

        public ExplosionEffect(IWalker creator, double radius) {
            Radius = radius;
            Position = creator.Position;
            CreationTime = DateTime.Now;
        }

        public bool MustDespawn {
            get {
                return DateTime.Now.Subtract(CreationTime) >= TimeSpan.FromSeconds(3);
            }
        }
    }
}
