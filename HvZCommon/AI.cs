using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZ.AI {
    public interface IHumanAI {
        void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ITakeSpace> resupply);
    }

    public interface IZombieAI {
        void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ITakeSpace> resupply);
    }

    public class RandomWalker : IHumanAI, IZombieAI {
        Random rng = new Random();

        void IHumanAI.DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ITakeSpace> resupply) {
            switch (rng.Next(50)) {
                case 0: player.TurnLeft(rng.NextDouble() * 300.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 300.0); break;
                case 2: player.GoForward(rng.NextDouble()*20.0); break;
            }
        }

        void IZombieAI.DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ITakeSpace> resupply) {
            switch (rng.Next(50)) {
                case 0: player.TurnLeft(rng.NextDouble() * 300.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 300.0); break;
                case 2: player.GoForward(rng.NextDouble()*20.0); break;
            }
        }
    }

}
