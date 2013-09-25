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
        Action<string> noAction;
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
            world.OnPlayerAdded += (_, __) => OnMapChange(this, EventArgs.Empty);
            world.OnPlayerRemoved += (_, __) => OnMapChange(this, EventArgs.Empty);
            connection.ConnectToServer("localhost");
            connection.Send(Command.NewCreate(map.RawMapData));
        }

        public ClientGame(System.Windows.Threading.Dispatcher dispatcher, string name, string role, Map map, AI.IHumanAI humanAI)
            : this(dispatcher, name, role, map) {
                requestDecision = () => humanAI.DoSomething(this, new List<IWalker>(map.Zombies), new List<IWalker>(map.Humans.Where(x => x.Id != connection.PlayerId)), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
                noAction = humanAI.Failure;
        }

        public ClientGame(System.Windows.Threading.Dispatcher dispatcher, string name, string role, Map map, AI.IZombieAI zombieAI)
            : this(dispatcher, name, role, map) {
            requestDecision = () => zombieAI.DoSomething(this, new List<IWalker>(map.Zombies.Where(x => x.Id != connection.PlayerId)), new List<IWalker>(map.Humans), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
            noAction = zombieAI.Failure;
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
            world.Eat(walkerId);
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

        void ICommandInterpreter.Human(uint walkerId, string name) {
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
            // If the player still exists, then then ask the AI to make decisions.
            if (map.walkers.ContainsKey(connection.PlayerId))
                requestDecision();
        }

        void ICommandInterpreter.No(string reason) {
            //throw new Exception(reason);
            noAction(reason);
        }

        void ICommandInterpreter.Right(uint walkerId, double degrees) {
            world.Right(walkerId, degrees);
        }

        void ICommandInterpreter.TakeFood(uint walkerId, uint resupplyId) {
            world.TakeFood(walkerId, resupplyId);
        }

        void ICommandInterpreter.TakeSocks(uint walkerId, uint resupplyId) {
            world.TakeSocks(walkerId, resupplyId);
        }

        void ICommandInterpreter.Throw(uint walkerId, double heading) {
            throw new NotImplementedException();
        }

        void ICommandInterpreter.Zombie(uint walkerId, string name) {
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


        SupplyItem[] IHumanPlayer.Inventory {
            get { return Map.humans[connection.PlayerId].Items; }
        }
    }

    public class PlayerAddedEventArgs : EventArgs {
        public uint PlayerId { get; set; }
    }
    public class PlayerRemovedEventArgs : EventArgs {
        public uint PlayerId { get; set; }
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
        public event EventHandler<PlayerAddedEventArgs> OnPlayerAdded;
        public event EventHandler<PlayerRemovedEventArgs> OnPlayerRemoved;
        public event EventHandler OnTurnEnded;

        public Game(Map m) {
            map = m;
        }

        public void Update() {
            try {
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
                // now ask the resupplypoints to replenish their stock, if they can.
                foreach (var r in map.ResupplyPoints) r.Update();
                // now check for death-by-timeout.
                // We do this now because it's possible for a walker to do something on the turn that they're about to die on (e.g. eat food or bite a victim).
                foreach (var w in map.walkers.ToArray()) {
                    if (w.Value.Lifespan > 0) continue;
                    // otherwise ... DEATH!
                    ongoing.Remove(w.Key);
                    map.Kill(w.Key);
                    if (OnPlayerRemoved != null)
                        OnPlayerRemoved(this, new PlayerRemovedEventArgs() { PlayerId = w.Key });
                }
                if (OnTurnEnded != null) OnTurnEnded(this, EventArgs.Empty);
            } catch (Exception e) {
                Console.WriteLine("Exception during world update!  Fix me!  {0}", e);
            }
        }

        public string Forward(uint walkerId, double dist) {
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
            return null;
        }

        public string Left(uint walkerId, double degrees) {
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
            return null;
        }

        public string Right(uint walkerId, double degrees) {
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
            return null;
        }

        // eating takes up a turn.
        public string Eat(uint walkerId) {
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game";
            var h = map.humans[walkerId];
            bool eaten = false;
            if (!h.Items.Contains(SupplyItem.Food)) return "you don't have any food in your inventory.";
            Action act = () => {
                if (eaten) return; // done.
                h.RemoveItem(SupplyItem.Food);
                h.Lifespan = h.MaximumLifespan;
                eaten = true;
            };
            ongoing[walkerId] = act;
            return null;
        }

        public string Bite(uint walkerId, uint target) {
            return "blah";
        }
        
        private string Take(uint walkerId, uint fromWhere, SupplyItem what) {
            var pt = map.ResupplyPoints.FirstOrDefault(x => x.Id == fromWhere);
            if (pt == null) return "the resupply point you've referred to doesn't exist";
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game"; // walker isn't a human, or doesn't exist.
            var w = map.humans[walkerId];
            if (!w.IsCloseEnoughToUse(pt)) return "you're still too far away from the resupply point to interact with it"; // too far away to interact with this.
            if (!pt.Available.Any(x => x == what)) return "the item you wanted to take isn't at this resupply point"; // the desired item doesn't exist here.
            if (!w.AddItem(what)) return "your inventory is already full"; // inventory is already full.
            pt.Remove(what);
            return null;
        }
        public string TakeFood(uint walkerId, uint fromWhere) {
            return Take(walkerId, fromWhere, SupplyItem.Food);
        }
        public string TakeSocks(uint walkerId, uint fromWhere) {
            return Take(walkerId, fromWhere, SupplyItem.Sock);
        }
        public string Throw(uint walkerId, double heading) {
            return "bleh";
        }
    }
}
