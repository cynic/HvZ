using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace HvZ.Common {
    public static class Utils {
        public static readonly Random rand = new Random();

        public static bool Intersects(this ITakeSpace a, ITakeSpace b) {
            // thanks to: http://stackoverflow.com/questions/8367512/algorithm-to-detect-if-a-circles-intersect-with-any-other-circle-in-the-same-pla
            var dX = a.Position.X - b.Position.X;
            var dY = a.Position.Y - b.Position.Y;
            var distBetween = Math.Sqrt((dX * dX) + (dY * dY));
            var sum = Math.Abs(a.Radius + b.Radius);
            var diff = Math.Abs(a.Radius - b.Radius);
            return distBetween >= diff && distBetween <= sum;
        }

        /// <summary>Calculates straight line distance between two Entities (untested)</summary>
        public static double DistanceFrom(this ITakeSpace a, ITakeSpace b) {
            double distX = a.Position.X - b.Position.X;
            double distY = a.Position.Y - b.Position.Y;
            return Math.Sqrt(distX * distX + distY * distY);
        }

        public static bool IsCloseEnoughToUse(this IWalker a, ITakeSpace b) {
            return a.DistanceFrom(b) - a.Radius - b.Radius <= WorldConstants.InteractionDistance;
        }

        /// <summary>Calculates heading needed to face one Entity from another (untested)</summary>
        public static double AngleTo(this IWalker a, ITakeSpace b) {
            /*
             * There is probably a better way (a one-liner??) to do this.  My trig's too weak to find it.  Instead,
             * I'm going to use a long and tedious way.  If you know a shorter way to do this, please kill what's
             * here and replace with something better.
             */
            var xA = a.Position.X;
            var yA = a.Position.Y;
            var xB = b.Position.X;
            var yB = b.Position.Y;
            var dxAB = xA -xB;
            var dyAB = yA - yB;
            double distAB = Math.Sqrt(dxAB * dxAB + dyAB * dyAB);
            var curHeading = (90.0 - a.Heading).ToRadians(); // relative to the x-axis
            // the heading intersects a circle with radius /distAB/ at some point, call it C.  Find that point of intersection.
            // x = xA + r cos (heading)
            // y = yA + r sin (heading)
            var xC = xA + distAB * Math.Cos(curHeading);
            var yC = yA + -distAB * Math.Sin(curHeading);
            // angle ACB = angle ABC.  I want angle CAB.
            // Bisect CB (call it D).  ACD is a right-angled triangle, where angle CAB = 2 * angle CAD.
            // note that distAB = distAC = r
            var dxCB = xC - xB;
            var dyCB = yC - yB;
            var distCD = Math.Sqrt(dxCB * dxCB + dyCB * dyCB) / 2.0;
            // now sin(angle CAD) = distCD / distAB, so angle CAD = arcsin(distCD / distAB)
            var angleRadians = Math.Asin(distCD / distAB) * 2.0;
            var angleDegrees = angleRadians.ToDegrees();
            var angle = (xA * yB - xB * yA < 0 ? 360 - angleDegrees : angleDegrees).PositiveAngle();
            return angle;
        }

        internal static double PositiveAngle(this double x) {
            return (360 + (x % 360)) % 360;
        }

        public static double ToDegrees(this double x) {
            return (x * 180.0) / Math.PI;
        }

        public static double ToRadians(this double x) {
            return (x * Math.PI) / 180.0;
        }

        public static T PickNext<T>(this T[] array) {
            return array[DateTime.Now.Second%array.Length];
        }

        public static T PickOne<T>(this T[] array) {
            return array[rand.Next(array.Length)];
        }

        public static T PickOne<T>(this IEnumerable<T> list) {
            return list.ElementAt(rand.Next(list.Count()));
        }

        public static T[] Tail<T>(this T[] array) {
            return array.Skip(1).ToArray();
        }

        public static IEnumerable<T> Tail<T>(this IEnumerable<T> array) {
            return array.Skip(1);
        }
    }
}
