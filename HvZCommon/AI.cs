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

    [Flags]
    public enum Edge {
        None = 0x0,
        Top = 0x1,
        Bottom = 0x2,
        Left = 0x4,
        Right = 0x8,
        TopAndLeft = Top | Left,
        TopAndRight = Top | Right,
        BottomAndLeft = Bottom | Left,
        BottomAndRight = Bottom | Right
    }

    public interface IHumanAI : AI {
        void Collision(IHumanPlayer player, ITakeSpace other);
        void Collision(IHumanPlayer player, Edge edge);
        void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public interface IZombieAI : AI {
        void Collision(IZombiePlayer player, ITakeSpace other);
        void Collision(IZombiePlayer player, Edge edge);
        void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

}
