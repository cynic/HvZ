using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    public class RandomWalker : IHumanAI
    {
        public string Name { get { return "Joker"; } }

        Random rng = new Random(7654);

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            if (player.Movement == MoveState.Moving) return;
            switch (rng.Next(2))
            {
                case 0: player.Turn((rng.NextDouble() > 0.5 ? -1 : 1) * rng.NextDouble() * 300.0); break;
                case 1: player.GoForward(rng.NextDouble() * 20.0); break;
            }
        }

        public void Failure(string what)
        {
            throw new Exception(what);
        }

        public void Collision(IHumanPlayer player, ITakeSpace other)
        {
            //Console.WriteLine("random human: I collided with {0}", other);
            player.Turn(player.AngleAvoiding(other));
        }

        public void Collision(IHumanPlayer player, Edge edge)
        {
            //Console.WriteLine("Hit the {0} edge of the map", edge);
        }
    }
}
