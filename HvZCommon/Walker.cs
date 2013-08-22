using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon
{
    public abstract class Walker : ITakeSpace
    {
        public Position Position { get; set; }
        public double Radius { get; set; }
        
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        public double Heading { get; set; }
        public double Speed { get; set; }

        public abstract string TextureName { get; }

        public WalkerType type { get; set; }

        private string _owner = "computer";
        public string Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (value != _owner)
                {
                    _owner = value;
                    if (!isPlayerControlled())
                    {
                        type |= WalkerType.SMART;
                    }
                }
            }
        }

        public bool isExploding()
        {
            return type.HasFlag(WalkerType.EXPLODING);
        }

        public bool isPlayerControlled()
        {
            return type.HasFlag(WalkerType.SMART);
        }

        public abstract void onKilled(Walker other);
    }

    public class Human : Walker
    {
        public override string TextureName
        {
            get { return "human"; }
        }

        public override void onKilled(Walker other)
        {
            //turn into a zombie
        }
        //does human things
    }

    public class Zombie : Walker
    {
        public override string TextureName
        {
            get { return "zombie"; }
        }

        public override void onKilled(Walker other)
        {
            //turn to something else. Maybe dirt
        }
        //does zombie things
    }
}
