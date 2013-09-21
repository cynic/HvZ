using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HvZ.Common {
    public interface ITakeSpace {
        Position Position { get; } // center of the object
        double Radius { get; }
        string Texture { get; }
    }

    public class Position : INotifyPropertyChanged {
        private double x, y;
        public double X {
            get { return x; }
            internal set { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("X")); x = value; }
        }
        public double Y {
            get { return y; }
            internal set { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Y")); y = value; }
        }

        public Position(double x, double y) {
            X = x;
            Y = y;
        }

        /// <summary>Calculates straight line distance between two Positions (untested)</summary>
        public double DistanceFrom(Position other) {
            double distX = X - other.X;
            double distY = Y - other.Y;

            return Math.Sqrt(distX * distX + distY * distY);
        }

        /// <summary>Calculates heading needed to face one Position from another (untested)</summary>
        public double AngleFrom(Position other) {
            double distX = X - other.X;
            double distY = Y - other.Y;
            double angle = Math.Asin(distX / DistanceFrom(other));

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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
