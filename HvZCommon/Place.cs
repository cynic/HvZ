using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
    public enum SupplyItem {
        NONE, FOOD, SOCK
    }

    /// <summary>
    /// Supply point provides food + health care.
    /// </summary>
    public class ResupplyPoint : ITakeSpace {
        public Position Position { get; set; }
        public double Radius { get; set; }
        public SupplyItem[] Available { get; set; }
        public void Remove(SupplyItem item) {
            throw new NotImplementedException();
        }
    }
}
