using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;

namespace HumansVsZombies
{
    class ShapeWalker : IHumanAI
    {
        int state = 0;
        Random rng = new Random();

        public void Collision(IHumanPlayer player, Edge edge)
        {
        }

        public void Collision(IHumanPlayer player, ITakeSpace other)
        {
        }

        public void DoSomething(IHumanPlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            if (player.Movement == MoveState.Moving) return;
            switch (state)
            {
                case 0 :
                    player.Turn(player.AngleToHeading(45));
                    state++;
                    break;
                case 1:
                case 3:
                case 5:
                case 7:
                case 9:
                case 11:
                case 13:
                case 15:
                    player.GoForward(5.0);
                    state++;
                    break;
                case 2:
                    player.Turn(player.AngleToHeading(90));
                    state++;
                    break;
                case 4:
                    player.Turn(player.AngleToHeading(90+45));
                    state++;
                    break;
                case 6:
                    player.Turn(player.AngleToHeading(180));
                    state++;
                    break;
                case 8:
                    player.Turn(player.AngleToHeading(180+45));
                    state++;
                    break;
                case 10:
                    player.Turn(player.AngleToHeading(270));
                    state++;
                    break;
                case 12:
                    player.Turn(player.AngleToHeading(270+45));
                    state++;
                    break;
                case 14:
                    player.Turn(player.AngleToHeading(0));
                    state++;
                    break;
                case 16:
                    player.Turn(rng.NextDouble() * 300.0);
                    state = 0;
                    break;
            }
        }

        public void Failure(string reason)
        {
        }

        public string Name
        {
            get { return "Shapely"; }
        }
    }
}
