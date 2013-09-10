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

    public interface IWalker : ITakeSpace {
        /// <summary>heading is in degrees, 0 is directly upwards</summary>
        double Heading { get; set; }
        double Speed { get; set; }
        int Health { get; set; }

        string Owner { get; set; }

        SpecialAbility ability { get; set; }

        bool isDead { get; set; }
        bool isHuman { get; }

        void TriggerSpecial();
        void Attack(IWalker attacker);

        event Killed OnKilled;
    }

    public class Human : IWalker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public int Health { get; set; }

        public string Owner { get; set; }

        public bool isHuman { get { return true; } }
        public string Texture { get { return "human"; } }

        public bool isDead { get; set; }
        public bool MustDespawn { get { return false; } }

        public SpecialAbility ability { get; set; }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }

        public void TriggerSpecial() {
        }

        public void Attack(IWalker other) {
            if (--Health == 0) {
                OnKilled(other);
            }
        }

        public Human(int health = -1) {
            Health = health >= 0 ? health : 20;
            ability = SpecialAbility.NULL;
            OnKilled += Human_OnKilled;
        }

        void Human_OnKilled(IWalker x) {
            respawnOwner = x.Owner;
            isDead = true;
        }

        private string respawnOwner;
        public IWalker ZombifiedVersion {
            get {
                if (isDead) {
                    return new Zombie() {
                        Position = Position,
                        Heading = Heading,
                        Radius = Radius,
                        Speed = Speed / 2,
                        Owner = respawnOwner
                    };
                }
                return this;
            }
        }
    }

    public class Zombie : IWalker {
        public double Heading { get; set; }
        public double Speed { get; set; }
        public int Health { get; set; }

        public string Owner { get; set; }

        public bool isHuman { get { return false; } }
        public string Texture { get { return "zombie"; } }

        public bool isDead { get; set; }
        public bool MustDespawn { get { return isDead; } }
        public SpecialAbility ability { get; set; }

        public event Killed OnKilled;

        public Position Position { get; set; }
        public double Radius { get; set; }

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

        public Zombie(int health = -1) {
            Health = health >= 0 ? health : 20;
            ability = SpecialAbility.NULL;
            OnKilled += Zombie_OnKilled;
        }

        void Zombie_OnKilled(IWalker x) {
            isDead = true;
        }

        public Zombie(int health, int type, Killed del) : this(health) {
                if (type == 1) {
                    setExploding(del);
                }
        }

        public void setExploding(Killed del) {
            if (!ability.HasFlag(SpecialAbility.EXPLODING)) {
                ability |= SpecialAbility.EXPLODING;
                OnKilled += del;
            }
        }
    }
}
