using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
   public enum SupplyItem {
      None, Sock, Food
   }

    public class ResupplyPoint : ITakeSpace {
        public Position Position { get; set; }
        public double Radius { get; set; }
        public SupplyItem[] Available { get; set; }
        public SupplyItem Remove(SupplyItem what) {
           throw new NotImplementedException();
        }
    }
}
