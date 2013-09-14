using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZ.Common {
    [Flags]
    public enum SpecialAbility {
        /// <summary>Value is not set</summary>
        NULL,
        /// <summary>Exploding Zombie</summary>
        EXPLODING,
    }

    public delegate void Killed(IWalker x);

    public interface IWalker : ITakeSpace, IIdentified {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; }
        string Name { get; }
        //double Speed { get; set; }
        //int Health { get; set; }

        //string Owner { get; set; }

        //SpecialAbility ability { get; set; }

        //bool isDead { get; set; }
        //bool isHuman { get; }

        //void TriggerSpecial();
        //void Attack(IWalker attacker);

        //event Killed OnKilled;
    }

    public class Human : IWalker {
        private Map map;
        public double Heading { get; set; }
        //public double Speed { get; set; }
        //public int Health { get; set; }
        public uint Id { get; private set; }

        //public string Owner { get; set; }

        //public bool isHuman { get { return true; } }
        public string Texture { get { return "human"; } }

        //public bool isDead { get; set; }
        //public bool MustDespawn { get { return false; } }

        //public SpecialAbility ability { get; set; }

        //public event Killed OnKilled;

        public Position Position { get; private set; }
        public double Radius { get; private set; }

        //public void TriggerSpecial() {
        //}

        /*
        public void Attack(IWalker other) {
            if (--Health == 0) {
                OnKilled(other);
            }
        }
        */

        public string Name { get; private set; }

        public Human(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
            //Health = health >= 0 ? health : 20;
            //ability = SpecialAbility.NULL;
            //OnKilled += Human_OnKilled;
        }

        /*
        void Human_OnKilled(IWalker x) {
            //respawnOwner = x.Owner;
            isDead = true;
        }
        */

        /*
        private string respawnOwner;
        public IWalker ZombifiedVersion {
            get {
                if (isDead) {
                    return new Zombie(Id) {
                        Position = Position,
                        Heading = Heading,
                        Radius = Radius,
                        Speed = Speed / 2,
                        //Owner = respawnOwner
                    };
                }
                return this;
            }
        }
        */
    }

    public class Zombie : IWalker {
        private Map map;
        public double Heading { get; private set; }
        //public double Speed { get; set; }
        public uint Id { get; private set; }
        public string Name { get; private set; }
        /*
        public string Owner { get; set; }

        public bool isHuman { get { return false; } }
        */
        public string Texture { get { return "zombie"; } }

        /*
        public bool isDead { get; set; }
        public bool MustDespawn { get { return isDead; } }
        public SpecialAbility ability { get; set; }

        public event Killed OnKilled;
        */

        public Position Position { get; set; }
        public double Radius { get; set; }

        /*
        public void TriggerSpecial() {
            if (ability.HasFlag(SpecialAbility.EXPLODING) && !isDead) {
                if (OnKilled != null) {
                    OnKilled(this);
                }
            }
        }

        public void Attack(IWalker other) {
            if (--Health == 0) {
                OnKilled(other);
            }
        }
        */

        public Zombie(uint id, string name, Map m, double x, double y, double heading) {
            Id = id;
            Name = name;
            map = m;
            Position = new Position(x, y);
            Heading = heading;
        }

        /*
        public void setExploding(Killed del) {
            if (!ability.HasFlag(SpecialAbility.EXPLODING)) {
                ability |= SpecialAbility.EXPLODING;
                OnKilled += del;
            }
        }
        */
    }
}
