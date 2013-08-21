using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
    [Flags]
    public enum WalkerType {
        /// <summary>Value is not set</summary>
        NULL,
        /// <summary>Exploding Zombie</summary>
        EXPLODING,
        /// <summary>Controlled by player AI</summary>
        SMART
    }
}
