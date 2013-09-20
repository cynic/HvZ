using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZ.AI {
    public interface IHumanAI {
        void DoSomething(IHumanPlayer player, List<ITakeSpace> environment);
    }

    public interface IZombieAI {
        void DoSomething(IZombiePlayer player, List<ITakeSpace> environment);
    }

    public class RandomWalker : IHumanAI, IZombieAI {
        Random rng = new Random();

        public void DoSomething(IHumanPlayer player, List<ITakeSpace> environment) {
            player.GoForward(10.0);
            switch (rng.Next(3)) {
                case 0: player.TurnLeft(rng.NextDouble() * 200.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 200.0); break;
            }
        }

        public void DoSomething(IZombiePlayer player, List<ITakeSpace> environment) {
            player.GoForward(10.0);
            switch (rng.Next(3)) {
                case 0: player.TurnLeft(rng.NextDouble() * 200.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 200.0); break;
            }
        }
    }
}
