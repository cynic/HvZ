using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HvZ.Common {
    public interface IWalkerExtended : IWalker, IVisual, IIdentified, INotifyPropertyChanged { }

    public class Human : IWalkerExtended {
        internal const int HumanLifespan = 600; // e.g. 600 = 60 seconds at 0.1s per turn.
        internal const double HumanRadius = 0.95;
        private Map map;

        double heading;
        public double Heading {
            get { return heading; }
            internal set {
                heading = value.PositiveAngle();
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Heading"));
            }
        }

        public uint Id { get; private set; }
        public string Texture { get { return "human"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }

        public string Name { get; private set; }

        int lifespan = HumanLifespan;
        public int Lifespan {
            get { return lifespan; }
            internal set {
                lifespan = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Lifespan"));
            }
        }

        public int MaximumLifespan { get { return HumanLifespan; } }

        public Human(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = HumanRadius;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Zombie : IWalkerExtended {
        internal const int ZombieLifespan = 600; // e.g. 600 = 60s at 0.1s per turn
        internal const double ZombieRadius = 0.95;
        private Map map;

        double heading;
        public double Heading {
            get { return heading; }
            internal set {
                heading = value.PositiveAngle();
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Heading"));
            }
        }

        public uint Id { get; private set; }
        public string Name { get; private set; }

        int lifespan = ZombieLifespan;
        public int Lifespan {
            get { return lifespan; }
            internal set {
                lifespan = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Lifespan"));
            }
        }

        public int MaximumLifespan { get { return ZombieLifespan; } }

        public string Texture { get { return "zombie"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }

        public Zombie(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = ZombieRadius;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
