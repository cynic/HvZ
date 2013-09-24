using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HvZ.Common {
    public enum Terrain : byte { // this can only go up to 4 bits (which means 16 possible entries, incl. Empty=0), because of how it's sent over the wire
        Empty=0, Ground
    }

    public class Map {
        Terrain[] terrain;
        internal Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();
        internal Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        internal Dictionary<uint, IWalkerExtended> walkers = new Dictionary<uint, IWalkerExtended>();
        internal List<SpawnPoint> spawners = new List<SpawnPoint>();
        internal List<Obstacle> obstacles = new List<Obstacle>();
        internal List<ResupplyPoint> resupply = new List<ResupplyPoint>();

        public IEnumerable<Human> Humans { get { return humans.Values; } }
        public IEnumerable<Zombie> Zombies { get { return zombies.Values; } }
        public IEnumerable<IVisual> Obstacles {
            get {
                foreach (var o in obstacles) yield return o;
                foreach (var s in spawners) yield return s;
            }
        }
        public IEnumerable<ResupplyPoint> ResupplyPoints { get { return resupply; } }

        public string RawMapData { get; private set; }

        public IWalker Walker(uint id) {
            return walkers[id]; // this will throw if the id isn't found.  That's fine; if it happens, fix the bug.
        }

        internal bool IsHuman(uint id) {
            return humans.ContainsKey(id);
        }

        internal bool IsZombie(uint id) {
            return zombies.ContainsKey(id);
        }

        public void SetHeading(uint id, double newHeading) {
            if (humans.ContainsKey(id)) {
                humans[id].Heading = newHeading;
            } else {
                zombies[id].Heading = newHeading;
            }
        }

        public void SetPosition(uint id, double x, double y) {
            var walker = walkers[id];
            var pos = walker.Position;
            var oldX = pos.X;
            var oldY = pos.Y;
            pos.X = Math.Max(walker.Radius, Math.Min(Width - walkers[id].Radius, x));
            pos.Y = Math.Max(walker.Radius, Math.Min(Height - walkers[id].Radius, y));
            // double-check against walkers.
            foreach (var kvp in walkers) {
                if (kvp.Key == id) continue;
                if (kvp.Value.Intersects(walker)) {
                    // reset back to the old values.
                    pos.X = oldX;
                    pos.Y = oldY;
                }
            }
            // double-check against obstacles.
            foreach (var o in Obstacles) { // capital-O Obstacles.  Includes spawnpoints.
                if (o.Intersects(walker)) {
                    // reset back to the old values.
                    pos.X = oldX;
                    pos.Y = oldY;
                }
            }
        }

        public void Kill(uint id) {
            humans.Remove(id);
            zombies.Remove(id);
            var w = walkers[id];
            walkers.Remove(id);
            spawners.Add(new SpawnPoint(w.Position.X, w.Position.Y, Math.Max(Human.HumanRadius, Zombie.ZombieRadius)));
        }

        public bool AddHuman(uint id, string name) {
            if (spawners.Count == 0) return false; // too many already in-game.
            var spawnIdx = Math.Abs((int)id) % spawners.Count;
            var spawner = spawners[spawnIdx];
            var h = new Human(id, name, this, spawner.Position.X, spawner.Position.Y, 0);
            humans.Add(id, h);
            walkers.Add(id, h);
            spawners.RemoveAt(spawnIdx);
            return true;
        }

        public bool AddZombie(uint id, string name) {
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
            terrain = new Terrain[Width * Height];
            for (int row = 0; row < Height; ++row) {
                for (int column = 0; column < lines[row].Length; ++column) {
                    switch (Char.ToLower(lines[row][column])) {
                        case '.':
                        case 's': terrain[row * Width + column] = Terrain.Ground; break;
                        case ' ': terrain[row * Width + column] = Terrain.Empty; break;
                        case '#':
                            terrain[row * Width + column] = Terrain.Ground;
                            var large = new Obstacle(column, row, 0.9);
                            obstacles.Add(large);
                            break;
                        case '@':
                            terrain[row * Width + column] = Terrain.Ground;
                            var medium = new Obstacle(column, row, 0.45);
                            obstacles.Add(medium);
                            break;
                        case '!':
                            terrain[row * Width + column] = Terrain.Ground;
                            var small = new Obstacle(column, row, 0.15);
                            obstacles.Add(small);
                            break;
                        case 'x':
                            terrain[row * Width + column] = Terrain.Ground;
                            spawners.Add(new SpawnPoint(column, row, Math.Max(Human.HumanRadius, Zombie.ZombieRadius)));
                            break;
                        case 'r':
                            terrain[row * Width + column] = Terrain.Ground;
                            resupply.Add(new ResupplyPoint(column, row));
                            break;
                        default:
                            throw new System.NotImplementedException(String.Format("There's something wrong with the map: I don't know how to handle '{0}' characters.", lines[row][column]));
                    }
                }
            }
            // save the map data.
            RawMapData = String.Join("\n", lines);
        }

        public Map(string filename) {
            ReadMap(File.ReadAllLines(filename));
        }

        public Map(string[] lines) {
            ReadMap(lines);
        }

        public Terrain this[double whereX, double whereY] {
            get {
                if (whereX >= Width || whereY >= Height || whereX < 0.0 || whereY < 0.0) {
                    throw new IndexOutOfRangeException(String.Format("The index [{0}, {1}] is out of bounds for the map with width {2}, height {3}", whereX, whereY, Width, Height));
                }
                // well, we know it's in-bounds.
                //var topLeft = terrain[(int)Math.Floor(whereX) * Width + (int)Math.Floor(whereY)];
                //var topRight = terrain[(int)Math.Ceiling(whereX) * Width + (int)Math.Floor(whereY)];
                //var bottomLeft = terrain[(int)Math.Floor(whereX) * Width + (int)Math.Ceiling(whereY)];
                //var bottomRight = terrain[(int)Math.Ceiling(whereX) * Width + (int)Math.Ceiling(whereY)];
                var closest = terrain[(int)Math.Round(whereY) * Width + (int)Math.Round(whereX)];
                //if (topLeft == topRight && bottomLeft == bottomRight && topLeft == bottomLeft) return topLeft; // they all agree, anyway.
                //if (match(MapItem.Empty, MapItem.Empty, MapItem.Empty, MapItem.Empty)) return 
                return closest;
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
