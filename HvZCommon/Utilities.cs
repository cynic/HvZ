using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;

namespace HvZ.Common {
    public static class Utils {
        public static readonly Random rand = new Random();

        public static bool Intersects(this ITakeSpace a, ITakeSpace b) {
            return Intersects(a, b.Position.X, b.Position.Y, b.Radius);
        }

        public static bool Intersects(this ITakeSpace a, double x, double y, double radius) {
            // thanks to: http://stackoverflow.com/questions/8367512/algorithm-to-detect-if-a-circles-intersect-with-any-other-circle-in-the-same-pla
            var dX = a.Position.X - x;
            var dY = a.Position.Y - y;
            var distBetween = Math.Sqrt((dX * dX) + (dY * dY));
            var sum = Math.Abs(a.Radius + radius);
            var diff = Math.Abs(a.Radius - radius);
            return distBetween >= diff && distBetween <= sum;
        }

        /// <summary>Calculates straight line distance between two Entities (apparently tested)</summary>
        public static double DistanceFrom(this ITakeSpace a, ITakeSpace b) {
            double distX = a.Position.X - b.Position.X;
            double distY = a.Position.Y - b.Position.Y;
            return Math.Sqrt(distX * distX + distY * distY);
        }

        public static bool IsCloseEnoughToUse(this IWalker a, ITakeSpace b) {
            return a.DistanceFrom(b) - a.Radius - b.Radius <= WorldConstants.InteractionDistance;
        }

        /// <summary>Calculates the number of degrees of turn needed to face a particular thing</summary>
        /// <returns>The angle to turn.  A negative number means a left turn, a positive number means a right turn.</returns>
        public static double AngleTo(this IWalker a, ITakeSpace b) {
            /*
             * Thanks to Chris for inspiring me to fix this; my version didn't work, and his version didn't work either.
             * (Atleast for what Yusuf was wanting them to do. XD )
             * 
             * GreedyHuman fails on the Plentiful map with either implementation.
             * On the other hand, this version works well, gives left-or-right turns, and it's short & clean too.
             */
            double distToTarget = a.DistanceFrom(b);
            double curHeading = (90.0 - a.Heading).ToRadians(); // relative to the x-axis
            // the heading intersects a circle with radius /distAB/ at some point, call it C.  Find that point of intersection.
            // x = xA + r cos (heading)
            // y = yA + r sin (heading)
            double xC = a.Position.X + distToTarget * Math.Cos(curHeading);
            double yC = a.Position.Y + -distToTarget * Math.Sin(curHeading);
            // let the built-in vector math sort it all out :-).
            Vector vHeading = new Vector(a.Position.X - xC, a.Position.Y - yC);
            Vector vTarget = new Vector(a.Position.X - b.Position.X, a.Position.Y - b.Position.Y);
            return Vector.AngleBetween(vHeading, vTarget);
        }

        public static double AngleAwayFrom(this IWalker a, ITakeSpace b) {
            var angleTo = a.AngleTo(b);
            if (angleTo + 180.0 >= 180.0)
                return 180.0 - angleTo;
            return angleTo + 180.0;
        }

        public static double AngleAvoiding(this IWalker a, ITakeSpace b) {
            var angleTo = a.AngleTo(b);
            return angleTo + 90.0;
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
