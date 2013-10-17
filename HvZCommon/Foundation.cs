using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HvZ.Common {
    static class WorldConstants {
        internal const double WalkerRadius = 0.95; // shared by Human, Zombie, and Box
        // terrain covered per turn
        internal const double HumanSpeed = 0.45;
        internal const double ZombieSpeed = 0.5;
        // degrees rotated per turn
        internal const double HumanTurnRate = 20.0;
        internal const double ZombieTurnRate = 20.0;
        // Reduced by 1 per turn, so e.g. 600 = 60s wall-clock duration at 0.1s per turn
        internal const int HumanLifespan = 600;
        internal const int ZombieLifespan = 600;
        // Number of small steps within a single turn.  Improves resolution of movement, at the cost of some CPU time.
        internal const int StepsPerTurn = 15;
        // Determines food/weapon generation at ResupplyPoints.  An item is generated every /n/ turns, where n = ResupplyDelay.
        internal const int ResupplyDelay = 4;
        internal const int ResupplyPointCapacity = 6;
        // Distance at which two objects can interact.
        internal const double InteractionDistance = 0.25;
        // Maximum number of items that can be carried by a human.
        internal const int MaximumItemsCarried = 6;
        // How many turns missiles last
        internal const int MissileLifespan = 20;
        // How much terrain a missile covers per turn
        internal const double MissileSpeed = 0.65;
        internal const double MissileRadius = 0.1;
        // When a missile hits, how many turns is the target stunned for?
        internal const int StunLength = 60;
    }
}
