using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.Client;
using HvZ.Common;

namespace HvZ {
    public class Game {
        private ClientGame clientGame;
        GameWindow g;
        App app;
        readonly string server;
        private bool CNG_called, Join_called;
        private List<IZombieAI> zAI = new List<IZombieAI>();
        private List<IHumanAI> hAI = new List<IHumanAI>();

        public Game(string server = "146.231.122.45") {
            this.server = server;
            app = new App();
            app.InitializeComponent();
        }

        private void initializeGameWindow(string gameName) {
            app.Resources["gameName"] = gameName;
            try {
                clientGame = new ClientGame(gameName, server);
            } catch (System.Net.Sockets.SocketException) {
                throw new Exception("The game server has crashed (I can't connect to it, anyway...)! Find your lecturer and tell him. He'll restart it for you.");
            }
            app.Resources["clientGame"] = clientGame;
            g = new GameWindow(); // ... which uses the Resources I've just set up...
        }

        public void CreateNewGame(string gameName, Map m) {
            if (CNG_called) throw new InvalidOperationException("You can only call CreateNewGame once.");
            if (Join_called) throw new InvalidOperationException("You've already joined a game.");
            if (g == null) initializeGameWindow(gameName);
            clientGame.CreateGame(m);
            CNG_called = true;
        }

        public void Join(string gameName, IZombieAI ai) {
            if (g == null) initializeGameWindow(gameName);
            clientGame.AddZombie(ai);
            if (!CNG_called) Join_called = true;
        }

        public void Join(string gameName, IHumanAI ai) {
            if (g == null) initializeGameWindow(gameName);
            clientGame.AddHuman(ai);
            if (!CNG_called) Join_called = true;
        }

        public void Display() {
            app.Run();
        }

        public int Slots {
            get {
                return g.game.Map.spawners.Count;
            }
        }
    }
}
