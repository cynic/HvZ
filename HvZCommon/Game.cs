using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    class FailureEventArgs : EventArgs {
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

    class ClientGame : ICommandInterpreter, IDisposable {
        private HvZConnection connection = new HvZConnection();

        readonly Map map = new Map();
        public Map Map {
            get { return map; }
        }
        Action requestDecision;
        Action<uint, string> noAction;
        Action<uint, string> playerAdded;
        Game world;
        readonly string gameName;

        public int Width { get { return map.Width; } }
        public int Height { get { return map.Height; } }

        System.Windows.Threading.Dispatcher dispatcher; // stupid, stupid, stupid WPF.  *sigh*.

        /// <summary>
        /// Create a new game, using the given map.
        /// </summary>
        /// <param name="map"></param>
        public ClientGame(string gameName) {
            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            this.gameName = gameName;
            connection.ConnectToServer("localhost");
            // Setup complete.  Now receive the rest of the commands for the game.
            connection.OnCommandReceived += connection_OnGameCommand;
        }

        public void CreateGame(Map m) {
            map.PopulateFromSerializedData(m.GetSerializedData());
            world = new Game(map);
            world.OnPlayerAdded += (_, __) => { if (OnMapChange != null) OnMapChange(this, EventArgs.Empty); };
            world.OnPlayerRemoved += (_, __) => { if (OnMapChange != null) OnMapChange(this, EventArgs.Empty); };
            connection.Send(Command.NewCreate(gameName, map.GetSerializedData()));
        }

        public void AddZombie(IZombieAI ai) {
            var guid = Guid.NewGuid().ToString();
            Console.WriteLine("Using zombie guid={0}", guid);
            playerAdded += new Action<uint,string>((playerId, corr) => {
                if (corr != guid) return; // not the zombie we're looking for!
                var player = new ZombiePlayer(connection, playerId, map);
                var askAI = new Action(() => {
                    if (map.walkers.ContainsKey(playerId)) {
                        ai.DoSomething(player, new List<IWalker>(map.Zombies.Where(x => x.Id != playerId)), new List<IWalker>(map.Humans), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
                    }
                });
                requestDecision += askAI;
                world.OnPlayerRemoved += (_, args) => { if (args.PlayerId == playerId) requestDecision -= askAI; };
                noAction += (id, s) => { if (id == playerId) ai.Failure(s); };
                world.OnEntityCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.CollidedWith); };
                world.OnEdgeCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.Edge); };
            });
            connection.Send(Command.NewZombieJoin(gameName, guid, ai.Name));
        }

        public void AddHuman(IHumanAI ai) {
            var guid = Guid.NewGuid().ToString();
            Console.WriteLine("Using human guid={0}", guid);
            playerAdded += new Action<uint, string>((playerId, corr) => {
                if (corr != guid) return; // not the human we're looking for!
                var player = new HumanPlayer(connection, playerId, map);
                var askAI = new Action(() => {
                    if (map.walkers.ContainsKey(playerId)) {
                        ai.DoSomething(player, new List<IWalker>(map.Zombies), new List<IWalker>(map.Humans.Where(x => x.Id != playerId)), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
                    }
                });
                requestDecision += askAI;
                world.OnPlayerRemoved += (_, args) => { if (args.PlayerId == playerId) requestDecision -= askAI; };
                noAction += (id, s) => { if (id == playerId) ai.Failure(s); };
                world.OnEntityCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.CollidedWith); };
                world.OnEdgeCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.Edge); };
            });
            connection.Send(Command.NewHumanJoin(gameName, guid, ai.Name));
        }

        private void connection_OnGameCommand(object sender, CommandEventArgs e) {
            dispatcher.Invoke(new Action(() => Command.Dispatch(e.Command, this)));
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

        void ICommandInterpreter.JoinOK(uint walkerId, string guid, string mapData) {
            map.PopulateFromSerializedData(mapData);
            playerAdded(walkerId, guid);
            if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
        }

        void ICommandInterpreter.Turn(uint walkerId, double degrees) {
            world.Turn(walkerId, degrees);
        }

        void ICommandInterpreter.Move() {
            // first, do the moves.
            world.Update();
            // Ask the AIs to make decisions.
            if (requestDecision != null)
                requestDecision();
       }

        void ICommandInterpreter.GameNo(string reason) {
            throw new Exception(reason);
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

        public event EventHandler OnMapChange;

        void ICommandInterpreter.No(uint walkerId, string reason) {
            noAction(walkerId, reason);
        }

        void IDisposable.Dispose() {
            ((IDisposable)connection).Dispose();
        }
    }

    internal class PlayerAddedEventArgs : EventArgs {
        public uint PlayerId { get; set; }
    }
    internal class PlayerRemovedEventArgs : EventArgs {
        public uint PlayerId { get; set; }
    }
    internal class CollisionEventArgs : EventArgs {
        public uint PlayerId { get; set; }
        public ITakeSpace CollidedWith { get; set; }
    }
    internal class EdgeCollisionEventArgs : EventArgs {
        public uint PlayerId { get; set; }
        public Edge Edge { get; set; }
    }

    class Game {
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
        public event EventHandler<CollisionEventArgs> OnEntityCollision;
        public event EventHandler<EdgeCollisionEventArgs> OnEdgeCollision;
        public event EventHandler OnTurnEnded;

        public Game(Map m) {
            map = m;
            map.OnEntityCollision += (_, e) => { if (OnEntityCollision != null) OnEntityCollision(this, e); };
            map.OnEdgeCollision += (_, e) => { if (OnEdgeCollision != null) OnEdgeCollision(this, e); };
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
            map.SetMovementState(walkerId, MoveState.Moving);
            Action act = () => {
                if (distRemaining > 0.0) {
                    distRemaining -= Math.Min(distPerStep, distRemaining);
                    map.SetPosition(walkerId, walker.Position.X + distXPerStep, walker.Position.Y + distYPerStep);
                } else {
                    map.SetMovementState(walkerId, MoveState.Stopped);
                }
            };
            ongoing[walkerId] = act;
            return null;
        }

        public string Turn(uint walkerId, double degrees) {
            double turnRemaining = Math.Abs(degrees);
            double turnPerStep = (map.IsHuman(walkerId) ? WorldConstants.HumanTurnRate : WorldConstants.ZombieTurnRate) / WorldConstants.StepsPerTurn;
            var walker = map.Walker(walkerId);
            bool leftTurn = degrees < 0;
            map.SetMovementState(walkerId, MoveState.Moving);
            Action act = () => {
                if (turnRemaining > 0.0) {
                    double thisStep = Math.Min(turnRemaining, turnPerStep);
                    var newHeading = leftTurn ? walker.Heading - thisStep : walker.Heading + thisStep;
                    map.SetHeading(walkerId, newHeading);
                    turnRemaining = Math.Round(turnRemaining - thisStep, 5);
                } else {
                    map.SetMovementState(walkerId, MoveState.Stopped);
                }
            };
            ongoing[walkerId] = act;
            return null;
        }

        // eating takes up a turn.
        public string Eat(uint walkerId) {
            if (map.zombies.ContainsKey(walkerId)) return "zombies don't eat food";
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game";
            var h = map.humans[walkerId];
            bool eaten = false;
            if (!h.Items.Contains(SupplyItem.Food)) return "you don't have any food in your inventory.";
            map.SetMovementState(walkerId, MoveState.Stopped);
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
        
        // taking something takes up a turn
        private string Take(uint walkerId, uint fromWhere, SupplyItem what) {
            var pt = map.ResupplyPoints.FirstOrDefault(x => x.Id == fromWhere);
            if (pt == null) return "the resupply point you've referred to doesn't exist";
            if (map.zombies.ContainsKey(walkerId)) return "zombies can't use resupply points";
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game"; // walker isn't a human, or doesn't exist.
            var w = map.humans[walkerId];
            if (!w.IsCloseEnoughToUse(pt)) return "you're still too far away from the resupply point to interact with it"; // too far away to interact with this.
            if (!pt.Available.Any(x => x == what)) return "the item you wanted to take isn't at this resupply point"; // the desired item doesn't exist here.
            if (w.InventoryIsFull) return "your inventory is already full";
            bool taken = false;
            map.SetMovementState(walkerId, MoveState.Stopped);
            Action act = () => {
                if (taken) return;
                w.AddItem(what);
                pt.Remove(what);
                taken = true;
            };
            ongoing[walkerId] = act;
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
