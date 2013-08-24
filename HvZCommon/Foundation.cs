using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZCommon {
    public interface ITakeSpace {
        Position Position { get; set; } // center of the object
        double Radius { get; set; }
        string TextureName { get; }
    }

    public class Map {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<ITakeSpace> Children;
    }

    public class GameState {
        public Map Map { get; set; }
        public UInt32 GameTime { get; set; }
    }

    public class Position {
        public double X { get; set; }
        public double Y { get; set; }

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
