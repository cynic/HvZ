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
            switch (rng.Next(10)) {
                case 0: player.TurnLeft(rng.NextDouble() * 200.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 200.0); break;
                case 2: player.GoForward(10.0); break;
            }
        }
    }

    public class CurveRight : IHumanAI, IZombieAI {
        int i = 0;

        public void DoSomething(IHumanPlayer player, List<ITakeSpace> environment) {
            if (i++ % 2 == 0) player.TurnRight(5);
            else player.GoForward(0.5);
        }

        public void DoSomething(IZombiePlayer player, List<ITakeSpace> environment) {
            if (i++ % 2 == 0) player.TurnRight(5);
            else player.GoForward(0.5);
        }
    }

    public class CurrveLeft : IHumanAI, IZombieAI {
        int i = 0;

        public void DoSomething(IHumanPlayer player, List<ITakeSpace> environment) {
            if (i++ % 2 == 0) player.TurnLeft(5);
            else player.GoForward(0.5);
        }

        public void DoSomething(IZombiePlayer player, List<ITakeSpace> environment) {
            if (i++ % 2 == 0) player.TurnLeft(5);
            else player.GoForward(0.5);
        }
    }
}
