using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HvZ.Common {
    public interface IWalkerExtended : IWalker, IVisual, IIdentified, INotifyPropertyChanged { }

    public class Human : IWalkerExtended {
        public override string ToString() {
            var s = items.Count(x => x == SupplyItem.Sock);
            var f = items.Count(x => x == SupplyItem.Food);
            return String.Format("Human {0}, {1}, at {2} heading {3} with {4} socks & {5} food", Name, Movement, Position, Heading, s, f);
        }
        private Map map;

        double heading;
        public double Heading {
            get { return heading; }
            internal set {
                heading = value.PositiveAngle();
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Heading"));
            }
        }

        private List<SupplyItem> items = new List<SupplyItem>(WorldConstants.MaximumItemsCarried);
        public SupplyItem[] Items { get { return items.ToArray(); } }

        internal bool InventoryIsFull {
            get {
                return items.Count >= WorldConstants.MaximumItemsCarried;
            }
        }

        internal bool AddItem(SupplyItem what) {
            if (items.Count >= WorldConstants.MaximumItemsCarried) return false;
            items.Add(what);
            return true;
        }
        internal bool RemoveItem(SupplyItem what) {
            return items.Remove(what);
        }

        public uint Id { get; private set; }
        public string Texture { get { return "human"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }
        public string Name { get; private set; }

        public MoveState Movement { get; internal set; }

        int lifespan = WorldConstants.HumanLifespan;
        public int Lifespan {
            get { return lifespan; }
            internal set {
                lifespan = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Lifespan"));
            }
        }

        public int MaximumLifespan { get { return WorldConstants.HumanLifespan; } }

        public Human(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = WorldConstants.WalkerRadius;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Zombie : IWalkerExtended {
        public override string ToString() {
            return String.Format("Zombie {0}, {1}, at {2} heading {3}", Name, Movement, Position, Heading);
        }

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
        public MoveState Movement { get; internal set; }

        int lifespan = WorldConstants.ZombieLifespan;
        public int Lifespan {
            get { return lifespan; }
            internal set {
                lifespan = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Lifespan"));
            }
        }

        public int MaximumLifespan { get { return WorldConstants.ZombieLifespan; } }

        public string Texture { get { return "zombie"; } }

        public Position Position { get; set; }
        public double Radius { get; private set; }

        public Zombie(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            Radius = WorldConstants.WalkerRadius;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
