using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HvZCommon {
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
    }
}
