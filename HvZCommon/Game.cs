using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    public enum CGState {
        Invalid,
        CreateRequested,
        JoinRequested,
        InGame
    }

    public enum Role {
        Invalid, Human, Zombie
    }

    public class FailureEventArgs : EventArgs {
        public FailureEventArgs(string reason) {
            Reason = reason;
        }
        public string Reason { get; private set; }
    }

    /* Constructors:
     * The client gets to start a game or join an existing game.  In the case
     * of starting a game, the client can choose parameters.  In the case of
     * joining a game, the client has no say.
     */

    public class ClientGame : ICommandInterpreter {
        private HvZConnection connection = new HvZConnection();
        private string gameId = null;

        CGState state = CGState.Invalid;
        Role role = Role.Invalid;
        Map map;
        string playerName;

        public Groupes MapContents { get { return new Groupes(map); } }

        public int Width { get { return map.Width; } }
        public int Height { get { return map.Height; } }

        /// <summary>
        /// Create a new game, using the given map.
        /// </summary>
        /// <param name="map"></param>
        public ClientGame(string name, string role, Map map) {
            connection.OnCommandReceived += connection_OnGameCommand;
            switch (role) {
                case "Human": this.role = Role.Human; break;
                case "Zombie": this.role = Role.Zombie; break;
                default: throw new Exception("Not a human or a zombie; edit me to tell me what this is.");
            }
            playerName = name;
            state = CGState.CreateRequested;
            this.map = map;
            connection.ConnectToServer("localhost");
            connection.Send(Command.NewCreate(map.RawMapData));
        }

        private void connection_OnGameCommand(object sender, CommandEventArgs e) {
            Command.Dispatch(e.Command, this);
        }

        public void JoinGameAsHuman(string gameId, string name) {
            
        }

        public void JoinGameAsZombie(string gameId, string name) {
            connection.Send(Command.NewZombieJoin(gameId, name));
        }

        public event EventHandler<CommandEventArgs> OnGameCommand;
        public event EventHandler<FailureEventArgs> OnCommandFailure;

        void ICommandInterpreter.Create(string mapdata) {
            map = new Map(mapdata.Split('\r', '\n'));
        }

        void ICommandInterpreter.CreateOK(string gameId) {
            this.gameId = gameId;
        }

        void ICommandInterpreter.Eat(uint walkerId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Forward(uint walkerId, double distance) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Game(string gameId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Hello(uint walkerId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Human(uint walkerId, double x, double y, double heading) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.HumanJoin(string gameId, string name) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Left(uint walkerId, double degrees) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.ListEnd() {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.ListStart() {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Move() {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.No(string reason) {
            throw new Exception(reason);
        }

        void ICommandInterpreter.Right(uint walkerId, double degrees) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.TakeFood(uint walkerId, uint resupplyId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.TakeSocks(uint walkerId, uint resupplyId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Throw(uint walkerId, double heading) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Zombie(uint walkerId, double x, double y, double heading) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.ZombieJoin(string gameId, string name) {
            throw new NotImplementedException();
        }
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
