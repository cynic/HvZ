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

        private DateTime CreationTime { get; set; }

        public Position Position { get; set; }
        public double Radius { get; set; }
        public SupplyItem[] Available { get; set; }
        public string Texture { get { return "supply"; } }

        public void Remove(SupplyItem item) {
            throw new NotImplementedException();
        }

        public bool MustDespawn {
            get {
                return DateTime.Now.Subtract(CreationTime) == TimeSpan.FromMinutes(4);
            }
        }
    }
}