using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    enum State
    {
        Undecided,
        RunAway,
        Collided,
        HitZombie,
        FindFood,
        FindAmmo,
        GetFood,
        GetAmmo,
        AllDone,
        Dance0,
        Dance1
    }

    enum RunState
    {
        TopLeft, TopRight, BottomLeft, BottomRight
    }

    public class Taltos : IHumanAI
    {
        Random rand = new Random();

        State s;

        public void Collision(IHumanPlayer player, ITakeSpace other)
        {
            var angle = player.AngleAvoiding(other);
            player.Turn(angle + 2 * Math.Sign(angle));
            s = State.Collided;
        }

        IWalker GetClosestZombie(IHumanPlayer player, List<IWalker> zombies)
        {
            return zombies.OrderBy(x => x.DistanceFrom(player)).Where(x => !x.IsStunned).FirstOrDefault();
        }

        ResupplyPoint GetClosestResupplyWith(IHumanPlayer player, List<ResupplyPoint> resupply, SupplyItem what)
        {
            return resupply.OrderBy(x => x.DistanceFrom(player)).Where(x => x.Available.Contains(what)).FirstOrDefault();
        }

        Dictionary<uint, uint> targeted = new Dictionary<uint, uint>();
        uint gameTime = 0;

        RunState go_to;

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            gameTime++;
            if (player.Lifespan == 1) player.Eat();
            //if (zombies.Count == 0) return; // nothing to do!
            var closestZ =
                zombies
                    .OrderBy(x => x.DistanceFrom(player))
                    .Where(x => !x.IsStunned)
                    .Where(x => !targeted.ContainsKey(x.Id) || (gameTime - targeted[x.Id] > 8))
                    .FirstOrDefault();
            if (closestZ != null && player.IsInSockRange(closestZ) && player.Inventory.Contains(SupplyItem.Sock))
            {
                // screw it, throw a sock!
                player.Throw(player.Heading + player.AngleTo(closestZ));
                targeted[closestZ.Id] = gameTime;
                return;
            }
            if (player.Lifespan < player.MaximumLifespan / 2 && !player.Inventory.Contains(SupplyItem.Food) && gameTime % 20 == 0)
            {
                s = State.FindFood;
            }
            else if (!player.Inventory.Contains(SupplyItem.Sock) && gameTime % 50 == 0)
            {
                s = State.FindAmmo;
            }
            if (player.Movement == MoveState.Moving) return;
            Console.WriteLine("State: {0}", s);
            switch (s)
            {
                case State.Undecided: {
                    if (zombies.Count == 0) {
                        s = State.AllDone;
                        //s = State.RunAway;
                        return;
                    }
                    if (!player.Inventory.Contains(SupplyItem.Sock)) {
                        s = State.FindAmmo;
                    } else if (!player.Inventory.Contains(SupplyItem.Food)) {
                        s = State.FindFood;
                    } else {
                        go_to = (RunState)(((int)++go_to) % 4);
                        s = State.RunAway;
                    }
                    break;
                }
                case State.FindFood:
                case State.FindAmmo: {
                    var r = GetClosestResupplyWith(player, resupply, s == State.FindFood ? SupplyItem.Food : SupplyItem.Sock);
                    if (r == null) return; // ... aaand wait?
                    if (player.IsCloseEnoughToInteractWith(r)) {
                        s = s == State.FindFood ? State.GetFood : State.GetAmmo;
                        return;
                    }
                    var angle = player.AngleTo(r);
                    if (Math.Abs(angle) < 1.0) {
                        player.GoForward(player.DistanceFrom(r));
                    } else {
                        player.Turn(angle);
                    }
                    break;
                }
                case State.GetFood: {
                    var r = GetClosestResupplyWith(player, resupply, SupplyItem.Food);
                    if (r == null) {
                        s = State.Undecided;
                        return;
                    }
                    if (!player.IsCloseEnoughToInteractWith(r)) {
                        s = State.FindFood;
                        return;
                    }
                    Console.WriteLine("Available here: {0}", String.Join(", ", r.Available));
                    if (r.Available.Contains(SupplyItem.Food)) {
                        player.TakeFoodFrom(r);
                        s = State.Undecided;
                    }
                    break;
                }
                case State.GetAmmo: {
                    var r = GetClosestResupplyWith(player, resupply, SupplyItem.Sock);
                    if (r == null) {
                        s = State.Undecided;
                        return;
                    }
                    if (!player.IsCloseEnoughToInteractWith(r)) {
                        s = State.FindAmmo;
                        return;
                    }
                    Console.WriteLine("Available here: {0}", String.Join(", ", r.Available));
                    if (r.Available.Contains(SupplyItem.Sock) && player.InventorySlotsLeft > 1)
                    {
                        player.TakeSocksFrom(r);
                        return;
                    }
                    s = State.Undecided;
                    break;
                }
                case State.RunAway: {
                    // shift along the state, if we need to.
                    switch (go_to)
                    {
                        case RunState.TopLeft: if (player.Position.X <= player.MapWidth * 0.15 && player.Position.Y <= player.MapHeight * 0.15) go_to = RunState.TopRight; break;
                        case RunState.TopRight: if (player.Position.X >= player.MapWidth * 0.85 && player.Position.Y <= player.MapHeight * 0.15) go_to = RunState.BottomLeft; break;
                        case RunState.BottomLeft: if (player.Position.X <= player.MapWidth * 0.15 && player.Position.Y >= player.MapHeight * 0.85) go_to = RunState.BottomRight; break;
                        case RunState.BottomRight: if (player.Position.X >= player.MapWidth * 0.85 && player.Position.Y >= player.MapHeight * 0.85) go_to = RunState.TopLeft; break;
                    }
                    // check if we're angled correctly.
                    double angle = 0;
                    switch (go_to)
                    {
                        case RunState.TopLeft: angle = player.AngleToCoordinates(player.MapWidth * 0.1, player.MapHeight * 0.1); break;
                        case RunState.TopRight: angle = player.AngleToCoordinates(player.MapWidth * 0.9, player.MapHeight * 0.1); break;
                        case RunState.BottomLeft: angle = player.AngleToCoordinates(player.MapWidth * 0.1, player.MapHeight * 0.9); break;
                        case RunState.BottomRight: angle = player.AngleToCoordinates(player.MapWidth * 0.9, player.MapHeight * 0.9); break;
                    }
                    double dist = 0;
                    switch (go_to)
                    {
                        case RunState.TopLeft: dist = player.DistanceFrom(player.MapWidth * 0.1, player.MapHeight * 0.1); break;
                        case RunState.TopRight: dist = player.DistanceFrom(player.MapWidth * 0.9, player.MapHeight * 0.1); break;
                        case RunState.BottomLeft: dist = player.DistanceFrom(player.MapWidth * 0.1, player.MapHeight * 0.9); break;
                        case RunState.BottomRight: dist = player.DistanceFrom(player.MapWidth * 0.9, player.MapHeight * 0.9); break;
                    }
                    if (Math.Abs(angle) < 5.0)
                    {
                        player.GoForward(dist);
                    }
                    else
                    {
                        player.Turn(angle);
                    }
                    break;
                }
                case State.Collided: {
                    player.GoForward(rand.Next(1, 6));
                    s = State.Undecided;
                    break;
                }
                case State.AllDone: {
                    s = State.Dance0;
                    break;
                }
                case State.Dance0: player.Turn(20); s = State.Dance1; break;
                case State.Dance1: player.GoForward(6.0); s = State.Dance0; break;
            }
        }

        public void Failure(string reason)
        {
            Console.WriteLine("Game told Taltos: {0}", reason);
        }

        public string Name
        {
            get { return "Baronet Vladimir Taltos"; }
        }

        public void Collision(IHumanPlayer player, Edge edge)
        {
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
                    angle = player.AngleToHeading(90);
                    break;
                case Edge.Right:
                    angle = player.AngleToHeading(270);
                    break;
                case Edge.TopAndLeft:
                    angle = player.AngleToHeading(90 + 45);
                    break;
                case Edge.TopAndRight:
                    angle = player.AngleToHeading(180 + 45);
                    break;
                case Edge.BottomAndLeft:
                    angle = player.AngleToHeading(45);
                    break;
                case Edge.BottomAndRight:
                    angle = player.AngleToHeading(270 + 45);
                    break;
            }
            Console.WriteLine("Turning around {0} degrees", angle);
            player.Turn(angle);
            s = State.Collided;
        }
    }
}
