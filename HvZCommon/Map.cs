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
        internal List<SpawnPoint> spawners = new List<SpawnPoint>();
        internal List<Obstacle> obstacles = new List<Obstacle>();
        internal List<ResupplyPoint> resupply = new List<ResupplyPoint>();

        internal IEnumerable<Human> Humans { get { return humans.Values; } }
        internal IEnumerable<Zombie> Zombies { get { return zombies.Values; } }
        internal IEnumerable<IVisual> Obstacles {
            get {
                foreach (var o in obstacles) yield return o;
                foreach (var s in spawners) yield return s;
            }
        }
        internal IEnumerable<ResupplyPoint> ResupplyPoints { get { return resupply; } }

        internal event EventHandler<CollisionEventArgs> OnPlayerCollision;

        internal string RawMapData { get; private set; }

        internal IWalker Walker(uint id) {
            return walkers[id]; // this will throw if the id isn't found.  That's fine; if it happens, fix the bug.
        }

        internal bool IsHuman(uint id) {
            return humans.ContainsKey(id);
        }

        internal bool IsZombie(uint id) {
            return zombies.ContainsKey(id);
        }

        internal void CloseSlot() {
            if (spawners.Count == 0) return; // nothing to do.
            // WARNING: because of the next line, DO NOT call this while the game is running.
            // You will break synchronisation between clients, and then you're screwed...
            var spawner = spawners.PickOne();
            spawners.Remove(spawner);
            // replace with an obstacle instead.
            obstacles.Add(new Obstacle(spawner.Position.X, spawner.Position.Y, spawner.Radius));
        }

        internal void SetHeading(uint id, double newHeading) {
            if (humans.ContainsKey(id)) {
                humans[id].Heading = newHeading;
            } else {
                zombies[id].Heading = newHeading;
            }
        }

        internal void SetMovementState(uint id, MoveState newState) {
            if (humans.ContainsKey(id)) {
                humans[id].Movement = newState;
            } else {
                zombies[id].Movement = newState;
            }
        }

        internal void SetPosition(uint id, double x, double y) {
            var walker = walkers[id];
            var pos = walker.Position;
            var oldX = pos.X;
            var oldY = pos.Y;
            var newX = Math.Max(walker.Radius, Math.Min(Width - walkers[id].Radius, x));
            var newY = Math.Max(walker.Radius, Math.Min(Height - walkers[id].Radius, y));
            // double-check against walkers.
            foreach (var kvp in walkers) {
                if (kvp.Key == id) continue;
                if (kvp.Value.Intersects(newX, newY, walker.Radius)) {
                    SetMovementState(id, MoveState.Stopped);
                    if (OnPlayerCollision != null) OnPlayerCollision(this, new CollisionEventArgs() { CollidedWith = kvp.Value, PlayerId = id });
                    return;
                }
            }
            // double-check against obstacles.
            foreach (var o in Obstacles) { // capital-O Obstacles.  Includes spawnpoints.
                if (o.Intersects(newX, newY, walker.Radius)) {
                    SetMovementState(id, MoveState.Stopped);
                    if (OnPlayerCollision != null) OnPlayerCollision(this, new CollisionEventArgs() { CollidedWith = o, PlayerId = id });
                    return;
                }
            }
            pos.X = newX;
            pos.Y = newY;
        }

        internal void Kill(uint id) {
            humans.Remove(id);
            zombies.Remove(id);
            var w = walkers[id];
            walkers.Remove(id);
            spawners.Add(new SpawnPoint(w.Position.X, w.Position.Y, WorldConstants.WalkerRadius));
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
            // save the map data.
            RawMapData = String.Join("\n", lines);
        }

        public Map(string filename) {
            ReadMap(File.ReadAllLines(filename));
        }

        internal Map(string[] lines) {
            ReadMap(lines);
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
