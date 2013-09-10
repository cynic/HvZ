using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public static class Utils {
        internal static readonly Random rand = new Random();

        public static Position randPosition(int maxWidth, int maxHeight) {
            return new Position(rand.Next(maxWidth), rand.Next(maxHeight));
        }

        public static string validateFileName(string str) {
            foreach (char i in
                Path.GetInvalidFileNameChars())
                str = str.Replace(i.ToString(), "_");
            return str;
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
