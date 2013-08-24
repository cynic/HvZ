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

    public delegate void Killed(Walker x);

    public interface Walker : ITakeSpace {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; set; }
        double Speed { get; set; }
        string Owner { get; set; }
        event Killed OnKilled;
    }

    public class Human : Walker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public string Owner { get; set; }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }
    }

    public class Zombie : Walker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public string Owner { get; set; }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }
    }
}
