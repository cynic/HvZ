using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    public class Teldra : IZombieAI
    {
        bool collided = false;
        Random rand = new Random();

        public bool ThinkOutLoud { get; set; }

        public void Collision(IZombiePlayer player, ITakeSpace other)
        {
            var angle = player.AngleAvoiding(other);
            if (Math.Abs(angle) < 0.05)
            {
                // Turn directly the other way, and move off.
                angle = player.AngleAwayFrom(other);
            }
            if (ThinkOutLoud) Console.WriteLine("Collided with {0}!  Turning {1} degrees to avoid.", other, angle);
            player.Turn(angle);
            collided = true;
        }

        IWalker target = null;

        IWalker GetNewTarget(IZombiePlayer player, List<IWalker> humans)
        {
            if (ThinkOutLoud) Console.WriteLine("Choosing new target.  Humans are:");
            foreach (var h in humans)
            {
                if (ThinkOutLoud) Console.WriteLine(" - {0}", h);
            }
            var closest = humans.OrderBy(x => x.DistanceFrom(player)).First();
            if (ThinkOutLoud) Console.WriteLine("Chosen: {0}", target);
            return closest;
        }

        public void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            if (humans.Count == 0)
            {
                if (ThinkOutLoud) Console.WriteLine("Yay, I've caught 'em all!  Suck on that, Ash Ketchum.");
                return; // no humans? Then I have nothing to do.
            }
            // opportunity knocks but once: if I'm in-range of any humans -- whether they're my target or not -- bite 'em!
            foreach (var h in humans.Where(x => player.IsCloseEnoughToInteractWith(x)))
            {
                if (ThinkOutLoud) Console.WriteLine("Opportunity knocks!  Biting {0}", h);
                player.Bite(h);
                return;
            }
            if (target == null || !humans.Exists(x => x.Id == target.Id))
            { // find a new target.
                target = GetNewTarget(player, humans);
            }
            // change targets if anyone comes quite close to me.
            var possibleNewTarget = humans.Where(x => player.DistanceFrom(x) < player.DistanceFrom(target) / 2).FirstOrDefault();
            if (possibleNewTarget != null)
            {
                if (ThinkOutLoud) Console.WriteLine("Switching target from {0} to {1}", target, possibleNewTarget);
                target = possibleNewTarget;
                Chase(player);
                return;
            }
            if (player.Movement == MoveState.Moving) return; // punt! until we stop moving.
            if (collided)
            {
                var dist = rand.Next(4, (int)Math.Max(6.0, Math.Min(player.MapWidth, player.MapHeight) / 6));
                if (ThinkOutLoud) Console.WriteLine("Recovering from collision; going forward {0} units.", dist);
                player.GoForward(dist);
                collided = false;
                return;
            }
            // now: chase!
            Chase(player);
        }

        private void Chase(IZombiePlayer player)
        {
            var angle = player.AngleTo(target);
            if (Math.Abs(angle) < 25.0)
            {
                var dist = player.DistanceFrom(target);
                if (dist < 7.0)
                {
                    if (ThinkOutLoud) Console.WriteLine("Going forward {0} units, hopefully this will get me close enough to bite {1}", dist, target);
                    player.GoForward(dist);
                }
                else
                {
                    if (ThinkOutLoud) Console.WriteLine("Going forward {0} units (aiming for {1}), then I'll reevaluate my strategy.", dist, target);
                    player.GoForward(dist / 4);
                }
            }
            else
            {
                if (ThinkOutLoud) Console.WriteLine("Turning {0} degrees to face {1}", angle, target);
                player.Turn(angle);
            }
        }

        public void Failure(string reason)
        {
            if (ThinkOutLoud) Console.WriteLine("Game told Teldra: {0}", reason);
        }

        public string Name
        {
            get { return "Lady Teldra"; }
        }

        public void Collision(IZombiePlayer player, Edge edge)
        {
            Console.WriteLine("I hit the {0} edge of the map", edge);
            collided = true;
            double angle = 0;
            switch (edge)
            {
                case Edge.Top:
                    angle = player.AngleToHeading(180);
                    break;
                case Edge.Bottom:
                    angle = player.AngleToHeading(0);
                    break;
                case Edge.Left:
                    angle = player.AngleToHeading(270);
                    break;
                case Edge.Right:
                    angle = player.AngleToHeading(90);
                    break;
                case Edge.TopAndLeft:
                    angle = player.AngleToHeading(90+45);
                    break;
                case Edge.TopAndRight:
                    angle = player.AngleToHeading(180+45);
                    break;
                case Edge.BottomAndLeft:
                    angle = player.AngleToHeading(45);
                    break;
                case Edge.BottomAndRight:
                    angle = player.AngleToHeading(270+45);
                    break;
            }
            Console.WriteLine("I'll turn {0} degrees to get away from this edge.", angle);
            player.Turn(angle);
        }
    }
}
