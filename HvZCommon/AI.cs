using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZ.AI {
    public interface IHumanAI {
        void Failure(string reason);
        void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public interface IZombieAI {
        void Failure(string reason);
        void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public class RandomWalker : IHumanAI, IZombieAI {
        Random rng = new Random();

        void IHumanAI.DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply) {
            switch (rng.Next(50)) {
                case 0: player.TurnLeft(rng.NextDouble() * 300.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 300.0); break;
                case 2: player.GoForward(rng.NextDouble()*20.0); break;
            }
        }

        void IZombieAI.DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply) {
            switch (rng.Next(50)) {
                case 0: player.TurnLeft(rng.NextDouble() * 300.0); break;
                case 1: player.TurnRight(rng.NextDouble() * 300.0); break;
                case 2: player.GoForward(rng.NextDouble()*20.0); break;
            }
        }

        public void Failure(string what) {
            throw new Exception(what);
        }
    }

    public class GreedyHuman : IHumanAI {
        public void Failure(string what) {

        }

        List<uint> visited = new List<uint>();

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply) {
            // am I almost dead??  If so, eat something!
            if (player.Lifespan == 1) {
                player.Eat();
                return;
            }
            // find the closest resupply point.
            if (visited.Count == resupply.Count)
                visited.Clear(); // start again!
            if (resupply.Count == 0) return; // do nothing!
            var supplyPoint = resupply.OrderBy(x => x.DistanceFrom(player)).Where(x => !visited.Contains(x.Id)).First();
            // am I there already?
            if (supplyPoint.Intersects(player) && supplyPoint.Available.Length > 0) {
                if (supplyPoint.Available.Contains(SupplyItem.Food)) {
                    player.TakeFoodFrom(supplyPoint);
                } else {
                    player.TakeSocksFrom(supplyPoint);
                }
                visited.Add(supplyPoint.Id);
            } else {
                // I still need to go there...
                double angleTo = player.AngleTo(supplyPoint);
                if (angleTo >= 10.0) {
                    player.TurnRight(angleTo);
                } else if (angleTo <= -10.0) {
                    player.TurnLeft(Math.Abs(angleTo));
                } else {
                    player.GoForward(player.DistanceFrom(supplyPoint));
                }
            }
        }
    }

}
