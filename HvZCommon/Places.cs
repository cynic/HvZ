using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public enum SupplyItem {
        NONE, FOOD, SOCK
    }

    /// <summary>
    /// Supply point provides food + health care.
    /// </summary>
    public class ResupplyPoint : ITakeSpace {
        //private DateTime CreationTime { get; set; }
        public Position Position { get; private set; }
        public double Radius { get; private set; }
        public ResupplyPoint(int x, int y) {
            Radius = 5.0;
            Position = new Position(x, y);
        }

        public SupplyItem[] Available { get; private set; }

        public string Texture { get { return "supply"; } }

        public void Remove(SupplyItem item) {
            throw new NotImplementedException();
        }
        /*
        public bool MustDespawn {
            get {
                return DateTime.Now.Subtract(CreationTime) == TimeSpan.FromMinutes(4);
            }
        }
        */
    }
}