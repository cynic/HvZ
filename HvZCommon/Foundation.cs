using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZ.Common {
    public interface ITakeSpace {
        Position Position { get; } // center of the object
        double Radius { get; }
        string Texture { get; }
        //bool MustDespawn { get; }
    }

    /// <summary>
    /// Provides a filtered view of the world
    /// </summary>
    public class Groupes {
        public Human[] Humans { get; private set; }
        public Zombie[] Zombies { get; private set; }
        public ResupplyPoint[] SupplyPoints { get; private set; }
        public Obstacle[] Obstacles { get; private set; }
        public ITakeSpace[] Uncategorized { get; private set; }

        public Groupes(List<ITakeSpace> items) {
            List<Human> humans = new List<Human>();
            List<Zombie> zombies = new List<Zombie>();
            List<ResupplyPoint> points = new List<ResupplyPoint>();
            List<Obstacle> obstacles = new List<Obstacle>();
            List<ITakeSpace> other = new List<ITakeSpace>();

            /*
            foreach (ITakeSpace i in items) {
                if (!i.MustDespawn || (i is IWalker && !((IWalker)i).isDead)) {
                    if (i is Human) {
                        humans.Add((Human)i);
                    } else if (i is Zombie) {
                        zombies.Add((Zombie)i);
                    } else if (i is Obstacle) {
                        obstacles.Add((Obstacle)i);
                    } else if (i is ResupplyPoint) {
                        points.Add((ResupplyPoint)i);
                    } else {
                        other.Add(i);
                    }
                }
            }
            */

            Humans = humans.ToArray();
            Zombies = zombies.ToArray();
            SupplyPoints = points.ToArray();
            Obstacles = obstacles.ToArray();
            Uncategorized = other.ToArray();
        }
    }

    public class Position {
        public double X { get; internal set; }
        public double Y { get; internal set; }

        public Position(double x, double y) {
            X = x;
            Y = y;
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
