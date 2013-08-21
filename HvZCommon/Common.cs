using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HvZCommon
{
    public interface ITakesSpace
    {
        Position Position { get; set; } // center of the object
        double Radius { get; set; }
    }

    public class Walker : ITakesSpace
    {
        public Position Position { get; set; }
        public double Radius { get; set; }

        public WalkerType type { get; set; }

        public double Heading { get; set; }
        public double Speed { get; set; }

        public bool isExploding() {
            return type.HasFlag(WalkerType.EXPLODING);
        }

        public bool isPlayerControlled() {
            return type.HasFlag(WalkerType.SMART);
        }
    }

    // heading is in degrees, 0 is directly upwards
    public class Human : Walker
    {
        //does human things
    }

    public class Zombie : Walker
    {
        //does zombie things
    }

    public class Position
    {
       public double X { get; set; }
       public double Y { get; set; }
    }

    public class GameState
    {
       public Map Map { get; set; }
       public UInt32 GameTime { get; set; }
    }

    /// <summary>
    /// Supply point provides food + health care.
    /// </summary>
    public class ResupplyPoint : ITakesSpace
    {
        public Position Position { get; set; }
        public double Radius { get; set; }

        public void Supply()
        {
            //give stuff
        }
    }

    /// <summary>
    /// Sock Depo provides sock refills/ammunition
    /// </summary>
    public class SockDepo : ResupplyPoint
    {
        public override void Supply()
        {
            //give socks
        }
    }

    public class Map
    {
       public int Width { get; set; }
       public int Height { get; set; }
       public List<ITakesSpace> Children;
    }

    public class Obstacle : ITakesSpace
    {
       public Position Position { get; set; } // center of the object
       public double Radius { get; set; }
    }
}
