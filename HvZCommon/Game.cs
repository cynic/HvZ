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

        internal Dictionary<uint, Tuple<string, long>> scoreboard = new Dictionary<uint, Tuple<string, long>>();

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

        public event EventHandler HumansWin;
        public event EventHandler ZombiesWin;
        public event EventHandler Draw;

        /// <summary>
        /// Create a new game, using the given map.
        /// </summary>
        /// <param name="map"></param>
        public ClientGame(string gameName, string server, int port) {
            dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            this.gameName = gameName;
            world = new Game(map);
            map.OnMapChange += (_, __) => { if (OnMapChange != null) OnMapChange(this, EventArgs.Empty); };
            world.OnGameEnded += (_, args) => {
                switch (args.Result) {
                    case GameResult.HumansWin:
                        if (HumansWin != null)
                            HumansWin(this, EventArgs.Empty);
                        break;
                    case GameResult.ZombiesWin:
                        if (ZombiesWin != null)
                            ZombiesWin(this, EventArgs.Empty);
                        break;
                    case GameResult.Draw:
                        if (Draw != null)
                            Draw(this, EventArgs.Empty);
                        break;
                    default:
                        break;
                }
            };
            world.OnPlayerRemoved += (_, __) => { if (OnMapChange != null) OnMapChange(this, EventArgs.Empty); };
            connection.ConnectToServer(server, port);
            // Setup complete.  Now receive the rest of the commands for the game.
            connection.OnCommandReceived += connection_OnGameCommand;
        }

        public void CreateGame(Map m) {
            map.PopulateFromSerializedData(m.GetSerializedData());
            connection.Send(Command.NewCreate(gameName, map.GetSerializedData()));
        }

        public void AddZombie(IZombieAI ai) {
            var guid = Guid.NewGuid().ToString();
            playerAdded += new Action<uint,string>((playerId, corr) => {
                if (corr != guid) return; // not the zombie we're looking for!
                var player = new ZombiePlayer(connection, playerId, map);
                var askAI = new Action(() => {
                    if (map.zombies.ContainsKey(playerId)) {
                        if (!map.zombies[playerId].IsStunned) {
                            ai.DoSomething(player, new List<IWalker>(map.Zombies.Where(x => x.Id != playerId)), new List<IWalker>(map.Humans), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
                        }
                    }
                });
                requestDecision += askAI;
                world.OnPlayerRemoved += (_, args) => {
                    if (args.PlayerId != playerId) return;
                    requestDecision -= askAI;
                    scoreboard[playerId] = Tuple.Create(scoreboard[playerId].Item1, (DateTime.Now - DateTime.FromBinary(scoreboard[playerId].Item2)).Ticks);
                };
                noAction += (id, s) => { if (id == playerId) ai.Failure(s); };
                world.OnEntityCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.CollidedWith); };
                world.OnEdgeCollision += (_, e) => { if (e.PlayerId == playerId) ai.Collision(player, e.Edge); };
            });
            connection.Send(Command.NewZombieJoin(gameName, guid, ai.Name));
        }

        public void AddHuman(IHumanAI ai) {
            var guid = Guid.NewGuid().ToString();
            playerAdded += new Action<uint, string>((playerId, corr) => {
                if (corr != guid) return; // not the human we're looking for!
                var player = new HumanPlayer(connection, playerId, map);
                var askAI = new Action(() => {
                    if (map.humans.ContainsKey(playerId)) {
                        ai.DoSomething(player, new List<IWalker>(map.Zombies), new List<IWalker>(map.Humans.Where(x => x.Id != playerId)), new List<ITakeSpace>(map.Obstacles), new List<ResupplyPoint>(map.ResupplyPoints));
                    }
                });
                requestDecision += askAI;
                world.OnPlayerRemoved += (_, args) => {
                    if (args.PlayerId != playerId) return;
                    requestDecision -= askAI;
                    scoreboard[playerId] = Tuple.Create(scoreboard[playerId].Item1, (DateTime.Now - DateTime.FromBinary(scoreboard[playerId].Item2)).Ticks);
                };
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
            world.Bite(walkerId, target);
        }

        void ICommandInterpreter.Forward(uint walkerId, double distance) {
            world.Forward(walkerId, distance);
        }

        void ICommandInterpreter.JoinOK(uint walkerId, string guid, string mapData) {
            map.PopulateFromSerializedData(mapData);
            playerAdded(walkerId, guid);
            scoreboard[walkerId] = Tuple.Create(map.walkers[walkerId].Name, DateTime.Now.ToBinary());
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

        void ICommandInterpreter.Throw(string missileId, uint walkerId, double heading) {
            world.Throw(missileId, walkerId, heading);
        }

        public event EventHandler OnMapChange;

        void ICommandInterpreter.No(uint walkerId, string reason) {
            noAction(walkerId, reason);
        }

        void IDisposable.Dispose() {
            ((IDisposable)connection).Dispose();
        }
    }

    internal class GameResultEventArgs : EventArgs {
        public GameResult Result { get; set; }
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

    enum GameResult {
        None, ZombiesWin, HumansWin, Draw
    }

    class Game {
        private Random rng = new Random(12345); // fixed seed, deliberately non-static.
        bool realGameStarted; // true if and only if both zombies & humans were once in the game.
        bool realGameEnded; // true if and only if both zombies & humans were once in the game, and now only one side is left.
        internal GameResult GameResult { get; private set; }

        private Map map;
        private Dictionary<uint, Func<bool>> ongoing = new Dictionary<uint, Func<bool>>();
        public event EventHandler<GameResultEventArgs> OnGameEnded;
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
            if (realGameStarted == false) {
                realGameStarted = map.humans.Count > 0 && map.zombies.Count > 0;
            }
            if (realGameEnded)
                return; // nothing to do!
            try {
                // ensure that all walkers are a bit closer to death.
                foreach (var h in map.Humans) if (h.Lifespan > 0) --h.Lifespan;
                HashSet<uint> exclude = new HashSet<uint>();
                foreach (var z in map.Zombies) {
                    if (z.Lifespan > 0)
                        --z.Lifespan;
                    if (z.IsStunned) {
                        --z.StunRemaining;
                        if (z.IsStunned) exclude.Add(z.Id);
                    }
                }
                // now do whatever is required on the turn.
                for (int i = 0; i < WorldConstants.StepsPerTurn; ++i) {
                    // permute order.
                    foreach (var key in ongoing.Keys.OrderBy(_ => rng.Next())) {
                        if (exclude.Contains(key))
                            continue;
                        // execute action.
                        try {
                            if (!ongoing[key]()) {
                                exclude.Add(key);
                                break;
                            }
                        } catch (Exception ex) {
                            Console.WriteLine(String.Format("Exception occurred for AI {0} ... fix it!\n{1}", map.walkers[key].Name, ex));
                            exclude.Add(key);
                        }
                    }
                }
                // now ask the resupplypoints to replenish their stock, if they can.
                foreach (var r in map.ResupplyPoints) r.Update();
                // now move any missiles on the map.  Done in a separate step to allow Matrix-like sidesteps ;) ... we'll see how that works out in practice, I guess.
                for (int i = 0; i < WorldConstants.StepsPerTurn; ++i) {
                    foreach (var missile in map.missiles.ToArray()) {
                        map.MoveMissile(missile);
                    }
                }
                // decrease missile lifespan.
                foreach (var missile in map.missiles.ToArray()) {
                    --missile.Lifespan;
                }
                // now check for death-by-timeout.
                // We do this now because it's possible for a walker to do something on the turn that they're about to die on (e.g. eat food or bite a victim).
                foreach (var w in map.walkers.ToArray()) {
                    if (w.Value.Lifespan > 0) continue;
                    // otherwise ... DEATH!
                    ongoing.Remove(w.Key);
                    map.Kill(w.Key);
                    if (OnPlayerRemoved != null) {
                        OnPlayerRemoved(this, new PlayerRemovedEventArgs() { PlayerId = w.Key });
                    }
                }
                // if the 'real' game has started, check for whether the game has ended.
                var humansLeft = map.humans.Count;
                var zombiesLeft = map.zombies.Count;
                if (realGameStarted && (humansLeft == 0 || zombiesLeft == 0)) {
                    if (humansLeft == 0 && zombiesLeft == 0) {
                        GameResult = Common.GameResult.Draw;
                    } else if (humansLeft > 0) {
                        GameResult = Common.GameResult.HumansWin;
                    } else { // zombiesLeft > 0
                        GameResult = Common.GameResult.ZombiesWin;
                    }
                    realGameEnded = true;
                    if (OnGameEnded != null)
                        OnGameEnded(this, new GameResultEventArgs() { Result = this.GameResult });
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
            Func<bool> act = () => {
                if (distRemaining > 0.0) {
                    distRemaining -= Math.Min(distPerStep, distRemaining);
                    return map.SetPosition(walkerId, walker.Position.X + distXPerStep, walker.Position.Y + distYPerStep);
                } else {
                    map.SetMovementState(walkerId, MoveState.Stopped);
                    return false; // don't need to do this .StepsPerTurn times
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
            Func<bool> act = () => {
                if (turnRemaining > 0.0) {
                    double thisStep = Math.Min(turnRemaining, turnPerStep);
                    var newHeading = leftTurn ? walker.Heading - thisStep : walker.Heading + thisStep;
                    turnRemaining = Math.Round(turnRemaining - thisStep, 5);
                    map.SetHeading(walkerId, newHeading);
                } else {
                    map.SetMovementState(walkerId, MoveState.Stopped);
                    return false;
                }
                return true;
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
            Func<bool> act = () => {
                if (eaten) return false; // done.
                h.RemoveItem(SupplyItem.Food);
                h.Lifespan = h.MaximumLifespan;
                eaten = true;
                return false;
            };
            ongoing[walkerId] = act;
            return null;
        }

        public string Bite(uint walkerId, uint target) {
            if (map.humans.ContainsKey(walkerId)) return "humans are too polite to bite things";
            if (!map.zombies.ContainsKey(walkerId)) return "your zombie has died before you could bite";
            if (map.zombies.ContainsKey(target)) return "you can't bite another zombie.";
            if (!map.humans.ContainsKey(target)) return "that human has already died.";
            var biter = map.walkers[walkerId];
            var bitee = map.walkers[target];
            if (!biter.IsCloseEnoughToInteractWith(bitee)) return "you're not close enough to bite that human.";
            bool bitten = false;
            map.SetMovementState(walkerId, MoveState.Stopped);
            Func<bool> act = () => {
                if (bitten) return false; // done.
                ongoing.Remove(target);
                map.Kill(target);
                map.zombies[walkerId].Lifespan = WorldConstants.ZombieLifespan;
                bitten = true;
                if (OnPlayerRemoved != null)
                    OnPlayerRemoved(this, new PlayerRemovedEventArgs() { PlayerId = target });
                return false;
            };
            ongoing[walkerId] = act;
            return null;
        }
        
        // taking something takes up a turn
        private string Take(uint walkerId, uint fromWhere, SupplyItem what) {
            var pt = map.ResupplyPoints.FirstOrDefault(x => x.Id == fromWhere);
            if (pt == null) return "the resupply point you've referred to doesn't exist";
            if (map.zombies.ContainsKey(walkerId)) return "zombies can't use resupply points";
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game"; // walker isn't a human, or doesn't exist.
            var w = map.humans[walkerId];
            if (!w.IsCloseEnoughToInteractWith(pt)) return "you're still too far away from the resupply point to interact with it"; // too far away to interact with this.
            if (!pt.Available.Any(x => x == what)) return "the item you wanted to take isn't at this resupply point"; // the desired item doesn't exist here.
            if (w.InventoryIsFull) return "your inventory is already full";
            bool taken = false;
            map.SetMovementState(walkerId, MoveState.Stopped);
            Func<bool> act = () => {
                if (taken) return false;
                w.AddItem(what);
                pt.Remove(what);
                taken = true;
                return false;
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
        public string Throw(string missileId, uint walkerId, double heading) {
            if (map.zombies.ContainsKey(walkerId)) return "zombies don't throw socks";
            if (!map.humans.ContainsKey(walkerId)) return "your human has been removed from the game";
            var h = map.humans[walkerId];
            if (!h.Items.Contains(SupplyItem.Sock))  return "you don't have any socks in your inventory.";
            bool thrown = false;
            map.SetMovementState(walkerId, MoveState.Stopped);
            Func<bool> act = () => {
                if (thrown) return false; // done.
                h.RemoveItem(SupplyItem.Sock);
                h.Lifespan = h.MaximumLifespan;
                var d = WorldConstants.WalkerRadius + WorldConstants.MissileRadius;
                var missileX = d * Math.Sin(heading.ToRadians());
                var missileY = d * Math.Cos(heading.ToRadians());
                map.AddMissile(new Missile(missileId, WorldConstants.MissileLifespan, h.Position.X + missileX, h.Position.Y - missileY, heading));
                thrown = true;
                return false;
            };
            ongoing[walkerId] = act;
            return null;
        }
    }
}
