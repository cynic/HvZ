using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HvZ.Common {
    public interface IWalker : ITakeSpace, IIdentified, INotifyPropertyChanged {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; }
        string Name { get; }
    }

    public class Human : IWalker {
        private Map map;
        double heading;
        public double Heading {
            get { return heading; }
            internal set { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Heading")); heading = value.PositiveAngle(); }
        }
        public uint Id { get; private set; }
        public string Texture { get { return "human"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }

        public string Name { get; private set; }

        public Human(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = 0.95;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Zombie : IWalker {
        private Map map;
        double heading;
        public double Heading {
            get { return heading; }
            internal set { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Heading")); heading = value.PositiveAngle(); }
        }
        public uint Id { get; private set; }
        public string Name { get; private set; }
        public string Texture { get { return "zombie"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }

        public Zombie(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = 0.95;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
