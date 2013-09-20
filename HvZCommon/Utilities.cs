using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public static class Utils {
        internal static readonly Random rand = new Random();

        public static bool Intersects(this ITakeSpace a, ITakeSpace b) {
            // thanks to: http://stackoverflow.com/questions/8367512/algorithm-to-detect-if-a-circles-intersect-with-any-other-circle-in-the-same-pla
            var dX = a.Position.X - b.Position.X;
            var dY = a.Position.Y - b.Position.Y;
            var distBetween = Math.Sqrt((dX * dX) + (dY * dY));
            var sum = Math.Abs(a.Radius + b.Radius);
            var diff = Math.Abs(a.Radius - b.Radius);
            return distBetween >= diff && distBetween <= sum;
        }

        public static double ToDegrees(this double x) {
            return (x * 180.0) / Math.PI;
        }

        public static double ToRadians(this double x) {
            return (x * Math.PI) / 180.0;
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
