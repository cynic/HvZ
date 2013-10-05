using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HvZ.Common {
    interface IVisual : ITakeSpace {
        string Texture { get; }
    }

    static class WorldConstants {
        internal const double WalkerRadius = 0.95; // shared by Human, Zombie, and Box
        // terrain covered per turn
        internal const double HumanSpeed = 0.45;
        internal const double ZombieSpeed = 0.4;
        // degrees rotated per turn
        internal const double HumanTurnRate = 20.0;
        internal const double ZombieTurnRate = 20.0;
        // Reduced by 1 per turn, so e.g. 600 = 60s wall-clock duration at 0.1s per turn
        internal const int HumanLifespan = 600;
        internal const int ZombieLifespan = 600;
        // Number of small steps within a single turn.  Improves resolution of movement, at the cost of some CPU time.
        internal const int StepsPerTurn = 25;
        // Determines food/weapon generation at ResupplyPoints.  An item is generated every /n/ turns, where n = ResupplyDelay.
        internal const int ResupplyDelay = 3;
        internal const int ResupplyPointCapacity = 6;
        // Distance at which two objects can interact.
        internal const double InteractionDistance = 0.25;
        // Maximum number of items that can be carried by a human.
        internal const int MaximumItemsCarried = 6;
    }
}
