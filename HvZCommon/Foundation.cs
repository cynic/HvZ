using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZCommon {
    public interface ITakeSpace {
        Position Position { get; set; } // center of the object
        double Radius { get; set; }
        string Texture { get; }
    }

    public class Map {
        public Map(int width, int height) {
            Width = width;
            Height = height;
            Children = new List<ITakeSpace>();
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public List<ITakeSpace> Children;

        public bool isInBounds(ITakeSpace item) {
            return (item.Position.X - item.Radius) >= 0 &&
                (item.Position.Y - item.Radius) >= 0 &&
                (item.Position.X + item.Radius) <= Width &&
                (item.Position.Y + item.Radius) <= Height;
        }

        public ITakeSpace ElementAt(int index) {
            if (index >= 0 && index < Children.Count) {
                return Children.ElementAt(index);
            }
            return null;
        }
    }

    public class Groupes {
        public Human[] Humans { get; private set; }
        public Zombie[] Zombies { get; private set; }
        public ResupplyPoint[] SupplyPoints { get; private set; }
        public Obstacle[] Obstacles { get; private set; }

        public Groupes(List<ITakeSpace> items) {
            List<Human> humans = new List<Human>();
            List<Zombie> zombies = new List<Zombie>();
            List<ResupplyPoint> points = new List<ResupplyPoint>();
            List<Obstacle> obstacles = new List<Obstacle>();

            foreach (ITakeSpace i in items) {
                if (i is Human) {
                    humans.Add((Human)i);
                } else if (i is Zombie) {
                    zombies.Add((Zombie)i);
                } else if (i is Obstacle) {
                    obstacles.Add((Obstacle)i);
                } else if (i is ResupplyPoint) {
                    points.Add((ResupplyPoint)i);
                }
            }

            Humans = humans.ToArray();
            Zombies = zombies.ToArray();
            SupplyPoints = points.ToArray();
            Obstacles = obstacles.ToArray();
        }
    }

    public class GameState {
        public GameState() {
            Dirty = false;
            Map = new Map(800,800);
            GameTime = 0;
        }

        public bool Dirty { get; set; }

        public void Spawn(ITakeSpace item) {
            if (Dirty) {
                Map.Children.Clear();
                Dirty = false;
            }

            if (item.Position == null || !Map.isInBounds(item)) {
                item.Position = Utils.randPosition(Map.Width, Map.Height);
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

            X = x;
            Y = y;

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
