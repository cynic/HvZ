using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZCommon;

namespace HvZClient {
    public delegate void Spawned(IWalker me);
    public delegate void Hungering(IWalker me);
    public delegate void Hit(IWalker me, IWalker attacker);
    public delegate void Walk(IWalker me, double distance);

    /// <summary>Do not use this class. Your AI class must extend either HumanAI or ZombieAI</summary>
    public class BaseAI {
        internal void Spawned(IWalker me) {
            if (OnSpawned != null) {
                OnSpawned(me);
            }
        }

        internal void Walked(IWalker me, double distance) {
            if (OnWalking != null) {
                OnWalking(me, distance);
            }
        }

        internal void Killed(IWalker me) {
            if (OnKilled != null) {
                OnKilled(me);
            }
        }

        internal void Hit(IWalker me, IWalker attacker) {
            if (OnHit != null) {
                OnHit(me, attacker);
            }
        }

        internal void Hungering(IWalker me) {
            if (OnHungry != null) {
                OnHungry(me);
            }
        }

        public event Spawned OnSpawned;
        public event Hungering OnHungry;
        public event Killed OnKilled;
        public event Hit OnHit;
        public event Walk OnWalking;
    }

    public enum AIType {
        ZOMBIE, HUMAN, DEFAULT
    }

    public class HumanAI : BaseAI {
        public HumanAI() {
            OnHungry += imHungry;
        }

        public void imHungry(IWalker me) {
            // gonna starve
        }
    }

    public class ZombieAI : BaseAI {
        public ZombieAI() {
            OnHungry += imHungry;
        }

        public void imHungry(IWalker me) {
            Human[] humans = Game.ThingsOnMap.Humans;
            Human closest = null;
            double close = 0;

            foreach (Human i in humans) {
                double dist = me.Position.distanceFrom(i.Position);
                if (dist < close) {
                    closest = i;
                    close = dist;
                }
            }

            if (closest == null) {
                closest = humans.PickOne();
            }

            double angleDif = me.Heading - closest.Position.angleFrom(me.Position);

            Game.Turn(me, angleDif);
            Game.Walk(me, close);
        }
    }
}
