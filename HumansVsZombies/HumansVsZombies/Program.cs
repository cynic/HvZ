using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;
using System.Text.RegularExpressions;

namespace HumansVsZombies
{
    class MyAI : IZombieAI
    {
        Random rng = new Random();

        public string Name
        {
            get
            {
                return "Your Name Here";
            }
        }

        public void Collision(IZombiePlayer player, Edge edge)
        {
            Console.WriteLine("Collided with edge {0}", edge);
        }

        public void Collision(IZombiePlayer player, ITakeSpace other)
        {
            Console.WriteLine("Collided with {0}", other);
            player.Turn(player.AngleAvoiding(other));
        }

        public void DoSomething(IZombiePlayer player, List<IWalker> zombies, List<IWalker> humans, List<ITakeSpace> obstacles, List<ResupplyPoint> resupply)
        {
            // this is what a RandomWalker does.  When asked, the RandomWalker just decides to do something ... random.
            // Your zombie gets to decide to do ONE THING ONLY every turn!  If you ask it to do more than one thing,
            // it's probable that only the last thing you request will actually be done.
            if (player.Movement == MoveState.Moving) return;
            switch (rng.Next(3))
            {
                case 0: player.Turn(rng.NextDouble() * -300.0); break;
                case 1: player.Turn(rng.NextDouble() * 300.0); break;
                case 2: player.GoForward(rng.NextDouble() * 20.0); break;
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
            // here is how you would start a new game
            Map m = new Map(@"YOUR_MAP_PATH_HERE");
            Game game = new Game();
            game.CreateNewGame("Dystopia", m); // choose your own name, instead of "
            game.Join("Dystopia", new GreedyHuman()); // add a GreedyHuman
            game.Join("Dystopia", new GreedyHuman()); // add a GreedyHuman
            game.Join("Dystopia", new GreedyHuman()); // add a GreedyHuman
            game.Join("Dystopia", new RandomWalker()); // add a RandomWalker
            game.Join("Dystopia", new RandomWalker()); // add a RandomWalker
            game.Join("Dystopia", new RandomWalker()); // add a RandomWalker
            //game.Join("Dystopia", new Teldra());
            game.Join("Dystopia", ai); // add my own AI
            game.Display();
            /* join an existing game, called 'Zanzibar' */
            //Game g = new Game();
            //g.Join("Zanzibar", ai);
            //g.Start();
        }
    }
}
