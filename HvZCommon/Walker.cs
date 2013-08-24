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

    public delegate void Killed(IWalker x);

    public interface IWalker : ITakeSpace {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; set; }
        double Speed { get; set; }
        string Owner { get; set; }
        bool isHuman { get; }

        event Killed OnKilled;
    }

    public class Human : IWalker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public string Owner { get; set; }
        public bool isHuman { get { return true; } }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }

        public Human(double x, double y) {
            Position = new Position(x, y);
        }
    }

    public class Zombie : IWalker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public string Owner { get; set; }
        public bool isHuman { get { return false; } }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }

        public Zombie(double x, double y) {
            Position = new Position(x, y);
        }
    }
}
