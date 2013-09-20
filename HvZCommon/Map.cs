using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HvZ.Common {
    public enum Terrain : byte { // this can only go up to 4 bits (which means 16 possible entries, incl. Empty=0), because of how it's sent over the wire
        Empty=0, Ground, Obstacle
    }

    public class Map {
        Terrain[] terrain;
        Dictionary<uint, Zombie> zombies = new Dictionary<uint, Zombie>();
        Dictionary<uint, Human> humans = new Dictionary<uint, Human>();
        Dictionary<uint, IWalker> walkers = new Dictionary<uint, IWalker>();
        List<ResupplyPoint> resupply = new List<ResupplyPoint>();
        public int PlayersAllowed { get; private set; }
        public int PlayersInGame { get; private set; }

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

        private void GetBodyForTransfer(out double xpos, out double ypos, out double heading) {
            // find something that's computer-controlled; it won't have a name.
            IWalker walker = walkers.Select(x => x.Value).FirstOrDefault(x => x.Name == null);
            if (walker == null) {
                throw new Exception("Shouldn't be here; map accounting has gone crazy.");
            }
            // Now steal its attributes and kill it.
            xpos = walker.Position.X;
            ypos = walker.Position.Y;
            heading = walker.Heading;
            if ((humans.Remove(walker.Id) || zombies.Remove(walker.Id)) && walkers.Remove(walker.Id)) {
                return;
            } else {
                throw new Exception("Map accounting is incorrect.  Fix it.");
            }
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
            pos.X = Math.Max(0.0, Math.Min(Width - walkers[id].Radius, x));
            pos.Y = Math.Max(0.0, Math.Min(Height - walkers[id].Radius, y));
            foreach (var kvp in walkers) {
                if (kvp.Key == id) continue;
                if (kvp.Value.Intersects(walker)) {
                    // reset back to the old values.
                    pos.X = oldX;
                    pos.Y = oldY;
                }
            }
        }

        public void SetHuman(uint id, uint x, uint y, double heading, string name) {
            // find the player with x,y coordinates specified.  Replace with this one.
            var w = walkers
                .First(walker => walker.Value.Position.X == x && walker.Value.Position.Y == y).Value;
            walkers.Remove(w.Id);
            humans.Remove(w.Id);
            zombies.Remove(w.Id);
            var h = new Human(id, name, this, x, y, heading);
            humans.Add(id, h);
            walkers.Add(id, h);
            PlayersInGame++;
        }

        public void SetZombie(uint id, uint x, uint y, double heading, string name) {
            // find the player with x,y coordinates specified.  Replace with this one.
            var w = walkers
                .First(walker => walker.Value.Position.X == x && walker.Value.Position.Y == y).Value;
            walkers.Remove(w.Id);
            humans.Remove(w.Id);
            zombies.Remove(w.Id);
            var z = new Zombie(id, name, this, x, y, heading);
            zombies.Add(id, z);
            walkers.Add(id, z);
            PlayersInGame++;
        }

        public bool AddHuman(uint id, string name) {
            if (PlayersInGame == PlayersAllowed) return false; // too many already in-game.
            double xpos, ypos, heading;
            try {
                GetBodyForTransfer(out xpos, out ypos, out heading);
            } catch (Exception e) {
                Console.WriteLine("AddHuman(): {0}", e);
                return false;
            }
            var h = new Human(id, name, this, xpos, ypos, heading);
            humans.Add(id, h);
            walkers.Add(id, h);
            PlayersInGame++;
            return true;
        }

        public bool AddZombie(uint id, string name) {
            // mostly copypasta from above
            if (PlayersInGame == PlayersAllowed) return false; // too many already in-game.
            double xpos, ypos, heading;
            try {
                GetBodyForTransfer(out xpos, out ypos, out heading);
            } catch (Exception e) {
                Console.WriteLine("AddZombie(): {0}", e);
                return false;
            }
            var z = new Zombie(id, name, this, xpos, ypos, heading);
            zombies.Add(id, z);
            walkers.Add(id, z);
            PlayersInGame++;
            return true;
        }

        private void ReadMap(string[] lines) {
            Width = lines.Max(x => x.Length);
            Height = lines.Length;
            terrain = new Terrain[Width * Height];
            var idstart = UInt32.MaxValue;
            for (int row = 0; row < Height; ++row) {
                for (int column = 0; column < lines[row].Length; ++column) {
                    switch (lines[row][column]) {
                        case '.':
                        case 's': terrain[row * Width + column] = Terrain.Ground; break;
                        case ' ': terrain[row * Width + column] = Terrain.Empty; break;
                        case '#': terrain[row * Width + column] = Terrain.Obstacle; break;
                        case 'x':
                            terrain[row * Width + column] = Terrain.Ground;
                            PlayersAllowed++;
                            // now throw in a player.
                            var id = idstart--;
                            if (PlayersAllowed % 2 == 0) {
                                var w = new Human(id, null, this, column, row, 0);
                                humans.Add(id, w);
                                walkers.Add(id, w);
                            } else {
                                var w = new Zombie(id, null, this, column, row, 0);
                                zombies.Add(id, w);
                                walkers.Add(id, w);
                            }
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
