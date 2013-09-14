using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    /* Constructors:
     * The client gets to start a game or join an existing game.  In the case
     * of starting a game, the client can choose parameters.  In the case of
     * joining a game, the client has no say.
     */
    public class ClientGame {
        private HvZConnection connection = new HvZConnection();

        public ClientGame() {
            connection.OnCommandReceived += (o, args) => OnGameCommand(this, args);
            connection.ConnectToServer("localhost");
        }

        public string CreateGame(Map map) {
            connection.Send(Command.NewCreate(map.RawMapData));
            return ""; // stub
        }

        public void JoinGameAsHuman(string gameId, string name) {
            connection.Send(Command.NewHumanJoin(gameId, name));
        }

        public void JoinGameAsZombie(string gameId, string name) {
            connection.Send(Command.NewZombieJoin(gameId, name));
        }

        public event EventHandler<CommandEventArgs> OnGameCommand;

    }

    public class Game {
        private Map map;
        private Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        private Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();

        public Game(Map m) {
            map = m;
        }

        public bool Forward(uint walkerId, double dist) {
            return false;
        }
        public bool Left(uint walkerId, double degrees) {
            return false;
        }
        public bool Right(uint walkerId, double degrees) {
            return false;
        }
        public bool Eat(uint walkerId) {
            return false;
        }
        public bool TakeFood(uint walkerId, uint fromWhere) {
            return false;
        }
        public bool TakeSocks(uint walkerId, uint fromWhere) {
            return false;
        }
        public bool Throw(uint walkerId, double heading) {
            return false;
        }
    }
}
