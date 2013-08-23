using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HvZCommon {
    public class Utils {
        public static string validateFileName(string str) {
            foreach (char i in
                Path.GetInvalidFileNameChars())
                str = str.Replace(i.ToString(), "_");
            return str;
        }
    }
}
