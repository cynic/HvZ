using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HvZ.Common;

namespace HvZ {
    public class Map {
        internal Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();
        internal Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        internal Dictionary<uint, IWalkerExtended> walkers = new Dictionary<uint, IWalkerExtended>();
        internal List<Missile> missiles = new List<Missile>();
        internal List<SpawnPoint> spawners = new List<SpawnPoint>();
        internal List<Obstacle> obstacles = new List<Obstacle>();
        internal List<ResupplyPoint> resupply = new List<ResupplyPoint>();
        public int Width { get; private set; }
        public int Height { get; private set; }

        internal IEnumerable<Human> Humans { get { return humans.Values; } }
        internal IEnumerable<Zombie> Zombies { get { return zombies.Values; } }
        internal IEnumerable<IVisual> Obstacles {
            get {
                foreach (var o in obstacles) yield return o;
                foreach (var s in spawners) yield return s;
            }
        }
        internal IEnumerable<IVisual> Missiles { get { return missiles; } }
        internal IEnumerable<ResupplyPoint> ResupplyPoints { get { return resupply; } }

        internal event EventHandler<CollisionEventArgs> OnEntityCollision;
        internal event EventHandler<EdgeCollisionEventArgs> OnEdgeCollision;
        internal event EventHandler OnMapChange;

        internal IWalker Walker(uint id) {
            return walkers[id]; // this will throw if the id isn't found.  That's fine; if it happens, fix the bug.
        }

        internal bool IsHuman(uint id) {
            return humans.ContainsKey(id);
        }

        internal bool IsZombie(uint id) {
            return zombies.ContainsKey(id);
        }

        internal void SetHeading(uint id, double newHeading) {
            if (humans.ContainsKey(id)) {
                humans[id].Heading = newHeading;
            } else {
                zombies[id].Heading = newHeading;
            }
        }

        internal void SetMovementState(uint id, MoveState newState) {
            //Console.WriteLine("Setting the movement state of {0} to {1}", id, newState);
            //Console.WriteLine(new System.Diagnostics.StackTrace());
            if (humans.ContainsKey(id)) {
                humans[id].Movement = newState;
            } else {
                zombies[id].Movement = newState;
            }
        }

        internal bool SetPosition(uint id, double x, double y) {
            var walker = walkers[id];
            var pos = walker.Position;
            var oldX = pos.X;
            var oldY = pos.Y;
            var edge = Edge.None;
            if (x < walkers[id].Radius) edge |= Edge.Left;
            if (x > Width - walkers[id].Radius) edge |= Edge.Right;
            if (y < walkers[id].Radius) edge |= Edge.Top;
            if (y > Height - walkers[id].Radius) edge |= Edge.Bottom;
            var newX = Math.Max(walker.Radius, Math.Min(Width - walkers[id].Radius, x));
            var newY = Math.Max(walker.Radius, Math.Min(Height - walkers[id].Radius, y));
            // double-check against walkers.
            foreach (var kvp in walkers) {
                if (kvp.Key == id) continue;
                if (kvp.Value.Intersects(newX, newY, walker.Radius)) {
                    SetMovementState(id, MoveState.Stopped);
                    // Exception: don't call when zombies bump into humans.
                    // This feature was a class request.
                    if (walker is Zombie && kvp.Value is Human) {
                        // ignore.
                    } else if (OnEntityCollision != null) OnEntityCollision(this, new CollisionEventArgs() { CollidedWith = kvp.Value, PlayerId = id });
                    return false;
                }
            }
            // double-check against obstacles.
            foreach (var o in Obstacles) { // capital-O Obstacles.  Includes spawnpoints.
                if (o.Intersects(newX, newY, walker.Radius)) {
                    SetMovementState(id, MoveState.Stopped);
                    if (OnEntityCollision != null) OnEntityCollision(this, new CollisionEventArgs() { CollidedWith = o, PlayerId = id });
                    return false;
                }
            }
            pos.X = newX;
            pos.Y = newY;
            if (edge != Edge.None && OnEdgeCollision != null) {
                OnEdgeCollision(this, new EdgeCollisionEventArgs() { PlayerId = id, Edge = edge });
            }
            return true;
        }

        internal void Kill(uint id) {
            humans.Remove(id);
            zombies.Remove(id);
            var w = walkers[id];
            walkers.Remove(id);
            spawners.Add(new SpawnPoint(w.Position.X, w.Position.Y, WorldConstants.WalkerRadius));
        }

        internal void AddMissile(Missile m) {
            missiles.Add(m);
            if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
        }

        internal bool AddHuman(uint id, string name) {
            if (spawners.Count == 0) return false; // too many already in-game.
            //Console.Error.WriteLine("Asked to put in {0}={1}", id, name);
            //Console.Error.WriteLine((new System.Diagnostics.StackTrace()).ToString());
            var spawnIdx = Math.Abs((int)id) % spawners.Count;
            var spawner = spawners[spawnIdx];
            var h = new Human(id, name, this, spawner.Position.X, spawner.Position.Y, 0);
            humans.Add(id, h);
            walkers.Add(id, h);
            spawners.RemoveAt(spawnIdx);
            return true;
        }

        internal bool AddZombie(uint id, string name) {
            // mostly copypasta from above
            if (spawners.Count == 0) return false; // too many already in-game.
            var spawnIdx = Math.Abs((int)id) % spawners.Count;
            var spawner = spawners[spawnIdx];
            var z = new Zombie(id, name, this, spawner.Position.X, spawner.Position.Y, 0);
            zombies.Add(id, z);
            walkers.Add(id, z);
            spawners.RemoveAt(spawnIdx);
            return true;
        }

        private void ReadMap(string[] lines) {
            Width = lines.Max(x => x.Length);
            Height = lines.Length;
            uint resupplyId = 0;
            for (int row = 0; row < Height; ++row) {
                for (int column = 0; column < lines[row].Length; ++column) {
                    switch (Char.ToLower(lines[row][column])) {
                        case '#':
                            var large = new Obstacle(column, row, 0.9);
                            obstacles.Add(large);
                            break;
                        case '@':
                            var medium = new Obstacle(column, row, 0.45);
                            obstacles.Add(medium);
                            break;
                        case '!':
                            var small = new Obstacle(column, row, 0.15);
                            obstacles.Add(small);
                            break;
                        case 'x':
                            spawners.Add(new SpawnPoint(column, row, WorldConstants.WalkerRadius));
                            break;
                        case 'r':
                            resupply.Add(new ResupplyPoint(++resupplyId, column, row));
                            break;
                        default:
                            // meh, just ignore it.
                            break;
                            //throw new System.NotImplementedException(String.Format("There's something wrong with the map: I don't know how to handle '{0}' characters.", lines[row][column]));
                    }
                }
            }
        }

        public Map(string filename) {
            ReadMap(File.ReadAllLines(filename));
        }

        internal string GetSerializedData() {
            // sorry, spanish people.  Maybe I'll fix this up some year.
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendFormat("wh:{0},{1}|", Width, Height);
            foreach (var z in zombies.Values)
                sb.AppendFormat("z:{0},{1},{2},{3},{4},{5}|", z.Id, z.Position.X, z.Position.Y, z.Lifespan, z.Heading, HvZ.Networking.Internal.toBase64(z.Name));
            foreach (var h in humans.Values) {
                sb.AppendFormat("h:{0},{1},{2},{3},{4},", h.Id, h.Position.X, h.Position.Y, h.Lifespan, h.Heading);
                foreach (var i in h.Items) sb.Append(i == SupplyItem.Food ? 'f' : 's');
                sb.AppendFormat(",{0}|",HvZ.Networking.Internal.toBase64(h.Name));
            }
            foreach (var s in spawners)
                sb.AppendFormat("sp:{0},{1},{2}|", s.Position.X, s.Position.Y, s.Radius);
            foreach (var o in obstacles)
                sb.AppendFormat("o:{0},{1},{2}|", o.Position.X, o.Position.Y, o.Radius);
            foreach (var r in resupply) {
                sb.AppendFormat("r:{0},{1},{2},", r.Id, r.Position.X, r.Position.Y);
                foreach (var i in r.Available) sb.Append(i == SupplyItem.Food ? 'f' : 's');
                sb.Append('|');
            }
            foreach (var m in missiles)
                sb.AppendFormat("m:{0},{1},{2},{3},{4}|", m.Id, m.Lifespan, m.Position.X, m.Position.Y, m.Heading);
            return sb.ToString();
        }

        internal void PopulateFromSerializedData(string serialized) {
            // sorry, spanish people.  Maybe I'll fix this up some year.
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            var dataItems = serialized.Split('|')
                .Where(x => x.Length > 0)
                .Select(x => x.Split(':'))
                .Select(x => new { Key=x[0], Values=x[1].Split(',') });
            // clear everything.
            zombies.Clear();
            humans.Clear();
            walkers.Clear();
            spawners.Clear();
            obstacles.Clear();
            resupply.Clear();
            missiles.Clear();
            // parse-back functions.  Not the most secure in the world, but time is pressing...
            Func<string,string,Position> parsePos = (a,b) => new Position(Double.Parse(a), Double.Parse(b));
            Action<string[]> Z = xs => {
                var id = UInt32.Parse(xs[0]);
                var x = Double.Parse(xs[1]);
                var y = Double.Parse(xs[2]);
                var life = Int32.Parse(xs[3]);
                var head = Double.Parse(xs[4]);
                var name = HvZ.Networking.Internal.fromBase64(xs[5]);
                var z = new Zombie(id, name, this, x, y, head);
                zombies.Add(id, z);
                walkers.Add(id, z);
            };
            Func<string,SupplyItem[]> parseItems = s => s.Select(x => x == 'f' ? SupplyItem.Food : SupplyItem.Sock).ToArray();
            Action<string[]> H = xs => {
                var id = UInt32.Parse(xs[0]);
                var x = Double.Parse(xs[1]);
                var y = Double.Parse(xs[2]);
                var life = Int32.Parse(xs[3]);
                var head = Double.Parse(xs[4]);
                var items = parseItems(xs[5]);
                var name = HvZ.Networking.Internal.fromBase64(xs[6]);
                var h = new Human(id, name, this, x, y, head);
                foreach (var i in items) h.AddItem(i);
                humans.Add(id, h);
                walkers.Add(id, h);
            };
            Action<string[]> SP = xs => {
                var x = Double.Parse(xs[0]);
                var y = Double.Parse(xs[1]);
                var r = Double.Parse(xs[2]);
                spawners.Add(new SpawnPoint(x, y, r));
            };
            Action<string[]> O = xs => {
                var x = Double.Parse(xs[0]);
                var y = Double.Parse(xs[1]);
                var r = Double.Parse(xs[2]);
                obstacles.Add(new Obstacle(x, y, r));
            };
            Action<string[]> R = xs => {
                var id = UInt32.Parse(xs[0]);
                var x = Int32.Parse(xs[1]);
                var y = Int32.Parse(xs[2]);
                var supply = parseItems(xs[3]);
                var r = new ResupplyPoint(id, x, y);
                r.stored.AddRange(supply);
                resupply.Add(r);
            };
            Action<string[]> M = xs => {
                var id = xs[0];
                var lifespan = Int32.Parse(xs[1]);
                var x = Double.Parse(xs[2]);
                var y = Double.Parse(xs[3]);
                var h = Double.Parse(xs[4]);
                var m = new Missile(id, lifespan, x, y, h);
                missiles.Add(m);
            };
            Action<string[]> WH = xs => {
                Width = Int32.Parse(xs[0]);
                Height = Int32.Parse(xs[1]);
            };
            // quick loop to process each item
            foreach (var di in dataItems) {
                switch (di.Key) {
                    case "z": Z(di.Values); break;
                    case "h": H(di.Values); break;
                    case "sp": SP(di.Values); break;
                    case "o": O(di.Values); break;
                    case "r": R(di.Values); break;
                    case "wh": WH(di.Values); break;
                    case "m": M(di.Values); break;
                    default: throw new Exception(String.Format("Map is corrupt? I don't know what the '{0}' key means.", di.Key));
                }
            }
            if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
        }

        internal Map() {
            // completely empty.
        }

        internal void MoveMissile(Missile missile) {
            missile = missiles.FirstOrDefault(m => m.Id == missile.Id);
            if (missile == null)
                return; // missile already destroyed, perhaps by collision with another missile.
            missile.Move();
            // hrmmmmm.  There may be an off-by-one in the lifespan calculation because of the lifespan check below...?
            // I wonder if I should care more.
            if (missile.Position.X < 0 || missile.Position.Y < 0 || missile.Position.X > Width || missile.Position.Y > Height || missile.Lifespan == 0) {
                missiles.Remove(missile); // out of sight, out of mind.
                if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
                return;
            }
            // double-check against walkers.
            foreach (var kvp in walkers) {
                if (kvp.Value.Intersects(missile)) {
                    // kill the missile, in any case.
                    missiles.Remove(missile);
                    if (IsZombie(kvp.Key)) {
                        ((Zombie)kvp.Value).Stun(); // stun it.  (I know, I'm stunned too.)
                    } // otherwise, if it's a human, leave it unaffected.  No friendly fire.
                    if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
                    return;
                }
            }
            // double-check against obstacles.
            foreach (var o in Obstacles) { // capital-O Obstacles.  Includes spawnpoints.
                if (o.Intersects(missile)) {
                    missiles.Remove(missile); // g'bye, missile.
                    if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
                    return;
                }
            }
            // and now check if it runs into any other missiles.  If so, destroy them both.
            foreach (var mx in missiles) {
                if (mx.Id == missile.Id) continue; // don't check for intersection against myself...
                if (missile.Intersects(mx)) {
                    missiles.Remove(missile);
                    missiles.Remove(mx);
                    if (OnMapChange != null) OnMapChange(this, EventArgs.Empty);
                    return;
                }
            }
        }
    }
}
