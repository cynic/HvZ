using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;
using HvZ.AI;

namespace HumansVsZombies
{
    class MyAI : IZombieAI
    {
        Random rng = new Random(7654);

        public string Name
        {
            get
            {
                return "Heroic Mooer";
            }
        }

        public void Collision(IZombiePlayer player, ITakeSpace other)
        {
            player.Turn(player.AngleAvoiding(other));
            //Console.WriteLine("Collided with something!");
        }

        public void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            if (player.Movement == MoveState.Moving) return;
            switch (rng.Next(2))
            {
                case 0: player.Turn((rng.NextDouble() > 0.5 ? -1 : 1) * rng.NextDouble() * 300.0); break;
                case 1: player.GoForward(rng.NextDouble() * 20.0); break;
            }
        }

        public void Failure(string reason)
        {
            Console.WriteLine("FAILURE: {0}", reason);
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            MyAI ai = new MyAI();
            /* start a new game, called 'Zanzibar' */
            Map m = new Map(@"C:\Users\Yusuf Motara\Projects\HvZ\HvZClient\Maps\Default map.txt");
            Game game = new Game();
            game.CreateNewGame("Porridge", m);
            game.Join("Porridge", new GreedyHuman());
            game.Join("Porridge", ai);
            game.Start();
            /* join an existing game, called 'Zanzibar' */
            //Game g = new Game();
            //g.Join("Whatever", ai);
            //g.Start();
        }
    }
}
