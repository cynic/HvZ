using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Common;

namespace HvZ.AI {
    public interface IHumanAI {
        void Failure(string reason);
        void Collision(IHumanPlayer player, ITakeSpace other);
        void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public interface IZombieAI {
        void Failure(string reason);
        void Collision(IZombiePlayer player, ITakeSpace other);
        void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply);
    }

    public class RandomWalker : IHumanAI, IZombieAI {
        Random rng = new Random();

        void IHumanAI.DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply) {
            if (player.Movement == MoveState.Moving) return;
            switch (rng.Next(2)) {
                case 0: player.Turn((rng.NextDouble() > 0.5 ? -1 : 1) * rng.NextDouble() * 300.0); break;
                case 1: player.GoForward(rng.NextDouble() * 20.0); break;
            }
        }

        void IZombieAI.DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply) {
            if (player.Movement == MoveState.Moving) return;
            switch (rng.Next(2)) {
                case 0: player.Turn((rng.NextDouble() > 0.5 ? -1 : 1) * rng.NextDouble() * 300.0); break;
                case 1: player.GoForward(rng.NextDouble() * 20.0); break;
            }
        }

        public void Failure(string what) {
            throw new Exception(what);
        }

        void IHumanAI.Collision(IHumanPlayer player, ITakeSpace other) {
        }

        void IZombieAI.Collision(IZombiePlayer player, ITakeSpace other) {
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
            // am I already moving?  If so, duck out until I've stopped.
            if (player.Movement == MoveState.Moving) {
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
                if (Math.Abs(angleTo) >= 10.0) {
                    player.Turn(angleTo);
                } else {
                    player.GoForward(player.DistanceFrom(supplyPoint));
                }
            }
        }

        public void Collision(IHumanPlayer player, ITakeSpace other) {
            var angle = player.AngleAvoiding(other);
            player.Turn(angle);
        }
    }

}
