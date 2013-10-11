using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZ {
    public interface AI {
        void Failure(string reason);
        string Name { get; }
    }

    public interface IHumanAI : AI {
        void Collision(IHumanPlayer player, ITakeSpace other);
        void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public interface IZombieAI : AI {
        void Collision(IZombiePlayer player, ITakeSpace other);
        void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

}
