using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZCommon
{
    public interface ITakeSpace
    {
        Position Position { get; set; } // center of the object
        double Radius { get; set; }
        string TextureName { get; }
    }

    public class Map
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<ITakeSpace> Children;
    }

    public class GameState
    {
        public Map Map { get; set; }
        public UInt32 GameTime { get; set; }
    }

    public class Position
    {
       public double X { get; set; }
       public double Y { get; set; }
    }
}
