using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
    public abstract class Place : ITakeSpace {
        public Position Position { get; set; }
        public double Radius { get; set; }
        public abstract string TextureName { get; }

        public abstract void Supply();
    }

    /// <summary>
    /// Supply point provides food + health care.
    /// </summary>
    public class ResupplyPoint : Place {
        public override string TextureName {
            get { return "supply"; }
        }

        public override void Supply() {
            //give stuff(food), doesn't always give socks
        }
    }

    /// <summary>
    /// Sock Depo provides sock refills/ammunition
    /// </summary>
    public class SockDepo : Place {
        public override string TextureName {
            get { return "depo"; }
        }

        public override void Supply() {
            //give socks only, uaslly just refill to maximum premitted
            //only serves humans
            //targeted by zombie
        }
    }
}
