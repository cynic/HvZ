using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZCommon {
    private enum TakeSpaceType {
        ZOMBIE, HUMAN, POINT
    }

    public interface ITakeSpace {
        Position Position { get; set; } // center of the object
        double Radius { get; set; }
        TakeSpaceType type { get; }
    }

    public class Map {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<ITakeSpace> Children;

        public bool isInBounds(ITakeSpace item) {
            return (item.Position.X - item.Radius) >= 0 &&
                (item.Position.Y - item.Radius) >= 0 &&
                (item.Position.X + item.Radius) <= Width &&
                (item.Position.Y + item.Radius) <= Height;
        }
    }

    public class Groupes {
        public Human[] Humans { get; private set; }
        public Zombie[] Zombies { get; private set; }
        public ResupplyPoint[] SupplyPoints { get; private set; }

        public Groupes(List<ITakeSpace> items) {
            List<Human> humans = new List<Human>();
            List<Zombie> zombies = new List<Zombie>();
            List<ResupplyPoint> points = new List<ResupplyPoint>();

            foreach (ITakeSpace i in items) {
                switch (i.type) {
                    case TakeSpaceType.HUMAN: humans.Add((Human)i);
                        break;
                    case TakeSpaceType.ZOMBIE: zombies.Add((Zombie)i);
                        break;
                    case TakeSpaceType.POINT: points.Add((ResupplyPoint)i);
                        break;
                }
            }

            Humans = humans.ToArray();
            Zombies = zombies.ToArray();
            SupplyPoints = points.ToArray();
        }
    }

    public class GameState {
        public bool Dirty { get; set; }

        public void Spawn(ITakeSpace item) {
            if (Dirty) {
                Map.Children.Clear();
                Dirty = false;
            }

            Map.Children.Add(item);
        }

        public Map Map { get; set; }
        public UInt32 GameTime { get; set; }
    }

    public class Position {
        public double X { get; set; }
        public double Y { get; set; }

        public Position(double x, double y) {
            X = x;
            Y = y;
        }

        public bool newPosition(string posX, string posY) {
            double x = X;
            double y = Y;

            try {
                x = Double.Parse(posX);
                y = Double.Parse(posY);
            } catch  {
                return false;
            }

            return true;
        }

        /// <summary>Calculates straight line distance between two Positions (untested)</summary>
        public double distanceFrom(Position other) {
            double distX = X - other.X;
            double distY = Y - other.Y;

            return Math.Sqrt(distX * distX + distY * distY);
        }

        /// <summary>Calculates heading needed to face one Position from another (untested)</summary>
        public double angleFrom(Position other) {
            double distX = X - other.X;
            double distY = Y - other.Y;
            double angle = Math.Asin(distX / distanceFrom(other));

            if (distX > 0) {
                angle = 360 - angle;
                if (distY < 0) {
                    angle -= 90;
                }
            } else if (distY < 0) {
                angle = 180 - angle;
            }
            
            return angle;
        }
    }
}
