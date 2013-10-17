using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    public class GreedyHuman : IHumanAI
    {
        Random rand = new Random();

        public string Name
        {
            get
            {
                return "Hoggish Greedly";
            }
        }

        public void Failure(string what)
        {

        }

        List<uint> visited = new List<uint>();
        bool hasCollidedLastTurn = false;

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            // am I almost dead??  If so, eat something!
            if (player.Lifespan == 1)
            {
                //Console.WriteLine("Almost dead -- eating something!");
                player.Eat();
                return;
            }
            // am I already moving?  If so, don't update my movement.
            if (player.Movement == MoveState.Moving)
            {
                return;
            }
            // I collided last turn, but I've finished my turning to avoid that now.  Go forward instead.
            if (hasCollidedLastTurn)
            {
                //Console.WriteLine("Collided last turn.  Finished turning, so going forward a bit.");
                player.GoForward(rand.Next(4,35));
                hasCollidedLastTurn = false;
                return;
            }
            // find the closest resupply point.
            if (visited.Count == resupply.Count)
            {
                visited.Clear(); // start again!
            }
            if (resupply.Count == 0)
            {
                //Console.WriteLine("No resupply points?  There's nothing for a greedy human to do, then :-(.  Will just sit and starve here...");
                return; // do nothing!
            }
            ResupplyPoint supplyPoint = null;
            foreach (ResupplyPoint r in resupply) {
                if ((supplyPoint == null || player.DistanceFrom(supplyPoint) > player.DistanceFrom(r)) && !visited.Contains(r.Id)) {
                    supplyPoint = r;
                }
            }
            // am I there already?
            if (supplyPoint.Intersects(player) && supplyPoint.Available.Length > 0)
            {
                if (supplyPoint.Available.Contains(SupplyItem.Food))
                {
                    //Console.WriteLine("Taking some food.");
                    player.TakeFoodFrom(supplyPoint);
                }
                else
                {
                    //Console.WriteLine("Taking some socks.");
                    player.TakeSocksFrom(supplyPoint);
                }
                visited.Add(supplyPoint.Id);
            }
            else
            {
                // I still need to go there...
                double angleTo = player.AngleTo(supplyPoint);
                if (Math.Abs(angleTo) >= 10.0)
                {
                    //Console.WriteLine("Turning {0} degrees to face my food, yum!");
                    player.Turn(angleTo);
                }
                else
                {
                    //Console.WriteLine("Going to my food.");
                    player.GoForward(player.DistanceFrom(supplyPoint));
                }
            }
        }

        public void Collision(IHumanPlayer player, ITakeSpace other)
        {
            if (player.Movement == MoveState.Moving) return; // nothing to do?
            double angle = player.AngleAvoiding(other);
            player.Turn(angle);
            //Console.WriteLine("Turning {0} degrees to avoid obstacle.", angle);
            hasCollidedLastTurn = true;
        }

        public void Collision(IHumanPlayer player, Edge edge)
        {
            
        }
    }
}
