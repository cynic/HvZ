using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using HvZ.AI;

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

    public class ClientGame : ICommandInterpreter, INotifyPropertyChanged, IHumanPlayer, IZombiePlayer {
        private HvZConnection connection = new HvZConnection();
        private string gameId = null;

        CGState state = CGState.Invalid;
        Role role = Role.Invalid;
        string playerName;

        Map map;
        public Map Map {
            get { return map; }
            internal set { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Map")); map = value; }
        }
        Action requestDecision;
        Game world;

        public int Width { get { return map.Width; } }
        public int Height { get { return map.Height; } }

        public bool isInBounds(ITakeSpace item) {
            return (item.Position.X - item.Radius) >= 0 &&
                (item.Position.Y - item.Radius) >= 0 &&
                (item.Position.X + item.Radius) <= Width &&
                (item.Position.Y + item.Radius) <= Height;
        }

        System.Windows.Threading.Dispatcher dispatcher; // stupid, stupid, stupid WPF.  *sigh*.

        /// <summary>
        /// Create a new game, using the given map.
        /// </summary>
        /// <param name="map"></param>
        private ClientGame(System.Windows.Threading.Dispatcher dispatcher, string name, string role, Map map) {
            connection.OnCommandReceived += connection_OnGameCommand;
            switch (role) {
                case "Human": this.role = Role.Human; break;
                case "Zombie": this.role = Role.Zombie; break;
                default: throw new Exception("Not a human or a zombie; edit me to tell me what this is.");
            }
            this.dispatcher = dispatcher;
            playerName = name;
            state = CGState.CreateRequested;
            this.Map = map;
            world = new Game(map);
            connection.ConnectToServer("localhost");
            connection.Send(Command.NewCreate(map.RawMapData));
        }

        public ClientGame(System.Windows.Threading.Dispatcher dispatcher, string name, string role, Map map, AI.IHumanAI humanAI)
            : this(dispatcher, name, role, map) {
                requestDecision = () => humanAI.DoSomething(this, new List<IWalker>(map.Zombies), new List<IWalker>(map.Humans.Where(x => x.Id != connection.PlayerId)), new List<ITakeSpace>(map.Obstacles), new List<ITakeSpace>(map.ResupplyPoints));
        }

        public ClientGame(System.Windows.Threading.Dispatcher dispatcher, string name, string role, Map map, AI.IZombieAI zombieAI)
            : this(dispatcher, name, role, map) {
            requestDecision = () => zombieAI.DoSomething(this, new List<IWalker>(map.Zombies.Where(x => x.Id != connection.PlayerId)), new List<IWalker>(map.Humans), new List<ITakeSpace>(map.Obstacles), new List<ITakeSpace>(map.ResupplyPoints));
        }

        private void connection_OnGameCommand(object sender, CommandEventArgs e) {
            dispatcher.Invoke(new Action(() => Command.Dispatch(e.Command, this)));
        }

        public void JoinGameAsHuman(string gameId, string name) {
            this.gameId = gameId;
            connection.Send(Command.NewHumanJoin(gameId, name));
        }

        public void JoinGameAsZombie(string gameId, string name) {
            this.gameId = gameId;
            connection.Send(Command.NewZombieJoin(gameId, name));
        }

        public event EventHandler<CommandEventArgs> OnGameCommand;
        public event EventHandler<FailureEventArgs> OnCommandFailure;

        void ICommandInterpreter.Create(string mapdata) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.CreateOK(string gameId) {
            this.gameId = gameId;
            switch (role) {
                case Role.Human: JoinGameAsHuman(gameId, playerName); break;
                case Role.Zombie: JoinGameAsZombie(gameId, playerName); break;
            }
        }

        void ICommandInterpreter.Eat(uint walkerId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Bite(uint walkerId, uint target) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Forward(uint walkerId, double distance) {
            world.Forward(walkerId, distance);
        }

        void ICommandInterpreter.Game(string gameId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Hello(uint walkerId) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Human(uint walkerId, double x, double y, double heading, string name) {
            map.SetHuman(walkerId, x, y, heading, name);
            if (OnPlayerJoin != null) OnPlayerJoin(this, EventArgs.Empty);
        }

        void ICommandInterpreter.Left(uint walkerId, double degrees) {
            world.Left(walkerId, degrees);
        }

        void ICommandInterpreter.ListEnd() {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.ListStart() {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Move() {
            // first, do the moves.
            world.Update();
            // then ask the AI to make decisions.
            requestDecision();
        }

        void ICommandInterpreter.No(string reason) {
            throw new Exception(reason);
        }

        void ICommandInterpreter.Right(uint walkerId, double degrees) {
            world.Right(walkerId, degrees);
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

        void ICommandInterpreter.Zombie(uint walkerId, double x, double y, double heading, string name) {
            map.SetZombie(walkerId, x, y, heading, name);
            if (OnPlayerJoin != null) OnPlayerJoin(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OnPlayerJoin;

        /*
   interface HvZ.AI.IHumanPlayer with
      member __.GoForward distance = send () (Forward (Option.get playerId, distance))
      member __.TurnLeft degrees = send () (Left (Option.get playerId, degrees))
      member __.TurnRight degrees = send () (Right (Option.get playerId, degrees))
      member __.Eat () = send () (Eat (Option.get playerId))
      member __.TakeFoodFrom (r : IIdentified) = send () (TakeFood (Option.get playerId, r.Id))
      member __.TakeSocksFrom (r : IIdentified) = send () (TakeSocks (Option.get playerId, r.Id))
      member __.Throw heading = send () (Throw (Option.get playerId, heading))
   
   interface HvZ.AI.IZombiePlayer with
      member __.GoForward distance = send () (Forward (Option.get playerId, distance))
      member __.TurnLeft degrees = send () (Left (Option.get playerId, degrees))
      member __.TurnRight degrees = send () (Right (Option.get playerId, degrees))
      member __.Eat target = send () (Bite (Option.get playerId, target.Id))
      member __.X : float with get
      member __.Y : float with get 
      member __.Heading : float with get 
      member __.X : float with get
      member __.Y : float with get 
         */

        void IHumanPlayer.Eat() {
            connection.Send(Command.NewEat(connection.PlayerId));
        }

        void IHumanPlayer.GoForward(double distance) {
            connection.Send(Command.NewForward(connection.PlayerId, distance));
        }

        void IHumanPlayer.TakeFoodFrom(IIdentified place) {
            connection.Send(Command.NewTakeFood(connection.PlayerId, place.Id));
        }

        void IHumanPlayer.TakeSocksFrom(IIdentified place) {
            connection.Send(Command.NewTakeSocks(connection.PlayerId, place.Id));
        }

        void IHumanPlayer.Throw(double heading) {
            connection.Send(Command.NewThrow(connection.PlayerId, heading));
        }

        void IHumanPlayer.TurnLeft(double degrees) {
            connection.Send(Command.NewLeft(connection.PlayerId, degrees));
        }

        void IHumanPlayer.TurnRight(double degrees) {
            connection.Send(Command.NewRight(connection.PlayerId, degrees));
        }

        void IZombiePlayer.Eat(IIdentified target) {
            connection.Send(Command.NewBite(connection.PlayerId, target.Id));
        }

        void IZombiePlayer.GoForward(double distance) {
            connection.Send(Command.NewForward(connection.PlayerId, distance));
        }

        void IZombiePlayer.TurnLeft(double degrees) {
            connection.Send(Command.NewLeft(connection.PlayerId, degrees));
        }

        void IZombiePlayer.TurnRight(double degrees) {
            connection.Send(Command.NewRight(connection.PlayerId, degrees));
        }


        double IHumanPlayer.Heading {
            get { return Map.humans[connection.PlayerId].Heading; }
        }

        double IHumanPlayer.MapHeight {
            get { return Map.Height; }
        }

        double IHumanPlayer.MapWidth {
            get { return Map.Width; }
        }

        double IHumanPlayer.X {
            get { return Map.humans[connection.PlayerId].Position.X; }
        }

        double IHumanPlayer.Y {
            get { return Map.humans[connection.PlayerId].Position.Y; }
        }


        double IZombiePlayer.Heading {
            get { return Map.zombies[connection.PlayerId].Heading; }
        }

        double IZombiePlayer.MapHeight {
            get { return Map.Height; }
        }

        double IZombiePlayer.MapWidth {
            get { return Map.Width; }
        }

        double IZombiePlayer.X {
            get { return Map.zombies[connection.PlayerId].Position.X; }
        }

        double IZombiePlayer.Y {
            get { return Map.zombies[connection.PlayerId].Position.Y; }
        }
    }

    public class Game {
        /* Assumptions:
         * - Humans move at 0.45 per turn.
         * - Zombies move at 0.4 per turn.
         * - Turn-rate for humans and zombies is 20 degrees per turn.
         */
        private Random rng = new Random(12345); // fixed seed, deliberately non-static.
        private const double humanMoveRate = 0.45;
        private const double zombieMoveRate = 0.4;
        private const double turnRate = 20.0;
        private const int stepsPerTurn = 25;

        private Map map;
        //private Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        //private Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();
        private Dictionary<uint, Action> ongoing = new Dictionary<uint, Action>();

        public Game(Map m) {
            map = m;
        }

        public void Update() {
            for (int i = 0; i < stepsPerTurn; ++i) {
                // permute order.
                foreach (var key in ongoing.Keys.OrderBy(_ => rng.Next())) {
                    // execute action.
                    ongoing[key]();
                }
            }
        }

        public bool Forward(uint walkerId, double dist) {
            double distRemaining = dist;
            double distPerStep = (map.IsHuman(walkerId) ? humanMoveRate : zombieMoveRate) / stepsPerTurn;
            var walker = map.Walker(walkerId);
            var distXPerStep = distPerStep * Math.Sin(walker.Heading.ToRadians());
            var distYPerStep = -distPerStep * Math.Cos(walker.Heading.ToRadians());
            Action act = () => {
                if (distRemaining > 0.0) {
                    distRemaining -= distPerStep;
                    map.SetPosition(walkerId, walker.Position.X + distXPerStep, walker.Position.Y + distYPerStep);
                }
            };
            ongoing[walkerId] = act;
            return true;
        }

        public bool Left(uint walkerId, double degrees) {
            double turnRemaining = degrees;
            double turnPerStep = turnRate / stepsPerTurn;
            var walker = map.Walker(walkerId);
            Action act = () => {
                if (turnRemaining > 0.0) {
                    turnRemaining -= turnPerStep;
                    map.SetHeading(walkerId, walker.Heading - turnPerStep);
                }
            };
            ongoing[walkerId] = act;
            return true;
        }

        public bool Right(uint walkerId, double degrees) {
            double turnRemaining = degrees;
            double turnPerStep = turnRate / stepsPerTurn;
            var walker = map.Walker(walkerId);
            Action act = () => {
                if (turnRemaining > 0.0) {
                    turnRemaining -= turnPerStep;
                    map.SetHeading(walkerId, walker.Heading + turnPerStep);
                }
            };
            ongoing[walkerId] = act;
            return true;
        }

        public bool Eat(uint walkerId) {
            return false;
        }
        public bool Bite(uint walkerId, uint target) {
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
