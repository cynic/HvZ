using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZCommon;

namespace HvZClient {
    
    /// <summary>Do not use this class. Your AI class must extend either HumanAI or ZombieAI</summary>
    public class BaseAI {
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
