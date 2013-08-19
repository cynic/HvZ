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

    // heading is in degrees, 0 is directly upwards
    public class Human : ITakesSpace
    {
       public Position Position { get; set; }
       public double Heading { get; set; }
       public double Radius { get; set; }
    }

    public class Zombie : ITakesSpace
    {
       public Position Position { get; set; }
       public double Heading { get; set; }
       public double Radius { get; set; }
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

    public class ResupplyPoint : ITakesSpace
    {
       public Position Position { get; set; }
       public double Radius { get; set; }
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
