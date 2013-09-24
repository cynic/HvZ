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
            internal set {
                map = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Map"));
            }
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
            world.OnMapChange += (_, __) => OnMapChange(this, EventArgs.Empty);
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
            map.AddHuman(walkerId, name);
            if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
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
            map.AddZombie(walkerId, name);
            if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OnMapChange;

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

        Position ITakeSpace.Position { get { return Map.walkers[connection.PlayerId].Position; } }
        double ITakeSpace.Radius { get { return Map.walkers[connection.PlayerId].Radius; } }
        double IWalker.Heading { get { return Map.walkers[connection.PlayerId].Heading; } }
        int IWalker.Lifespan { get { return Map.walkers[connection.PlayerId].Lifespan; } }
        string IWalker.Name { get { return Map.walkers[connection.PlayerId].Name; } }

        double IHumanPlayer.MapHeight { get { return Map.Height; } }
        double IHumanPlayer.MapWidth { get { return Map.Width; } }
        double IZombiePlayer.MapHeight { get { return Map.Height; } }
        double IZombiePlayer.MapWidth { get { return Map.Width; } }

        int IWalker.MaximumLifespan { get { return Map.walkers[connection.PlayerId].MaximumLifespan; } }
    }

    public class Game {
        /* Assumptions:
         * - Humans move at 0.45 per turn.
         * - Zombies move at 0.4 per turn.
         * - Turn-rate for humans and zombies is 20 degrees per turn.
         */
        private Random rng = new Random(12345); // fixed seed, deliberately non-static.

        private Map map;
        //private Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        //private Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();
        private Dictionary<uint, Action> ongoing = new Dictionary<uint, Action>();
        public event EventHandler OnMapChange;

        public Game(Map m) {
            map = m;
        }

        public void Update() {
            // ensure that all walkers are a bit closer to death.
            foreach (var h in map.Humans) if (h.Lifespan > 0) --h.Lifespan;
            foreach (var z in map.Zombies) if (z.Lifespan > 0) --z.Lifespan;
            // now do whatever is required on the turn.
            for (int i = 0; i < WorldConstants.StepsPerTurn; ++i) {
                // permute order.
                foreach (var key in ongoing.Keys.OrderBy(_ => rng.Next())) {
                    // execute action.
                    ongoing[key]();
                }
            }
            // now check for death-by-timeout.
            // We do this now because it's possible for a walker to do something on the turn that they're about to die on (e.g. eat food or bite a victim).
            foreach (var w in map.walkers.ToArray()) {
                if (w.Value.Lifespan > 0) continue;
                // otherwise ... DEATH!
                ongoing.Remove(w.Key);
                map.Kill(w.Key);
                if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
            }
        }

        public bool Forward(uint walkerId, double dist) {
            double distRemaining = dist;
            double distPerStep = (map.IsHuman(walkerId) ? WorldConstants.HumanSpeed : WorldConstants.ZombieSpeed) / WorldConstants.StepsPerTurn;
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
            double turnPerStep = (map.IsHuman(walkerId) ? WorldConstants.HumanTurnRate : WorldConstants.ZombieTurnRate) / WorldConstants.StepsPerTurn;
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
            double turnPerStep = (map.IsHuman(walkerId) ? WorldConstants.HumanTurnRate : WorldConstants.ZombieTurnRate) / WorldConstants.StepsPerTurn;
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
