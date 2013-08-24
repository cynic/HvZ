using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
   [Flags]
   public enum WalkerType {
      /// <summary>Value is not set</summary>
      NULL,
      /// <summary>Exploding Zombie</summary>
      EXPLODING,
      /// <summary>Controlled by player AI</summary>
      SMART
   }

   public delegate void Killed(IWalker x);

    public interface IWalker : ITakeSpace {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; set; }
        double Speed { get; set; }
        string Owner { get; set; }
        event Killed OnKilled;
    }

    public class Human : IWalker {
       public double Heading {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public double Speed {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public string Owner {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public event Killed OnKilled;

       public Position Position {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public double Radius {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }
    }

    public class Zombie : IWalker {
       public double Heading {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public double Speed {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public string Owner {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public event Killed OnKilled;

       public Position Position {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }

       public double Radius {
          get {
             throw new NotImplementedException();
          }
          set {
             throw new NotImplementedException();
          }
       }
    }
}
