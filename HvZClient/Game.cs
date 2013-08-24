using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZCommon;

namespace HvZClient {
    public class Game {

        public void Start(AI e) {
            //stub
        }

        public void HandleMessage(string pack) {
            //stub
        }

        private void Throw() {
            //stub
        }

        private void Walk(double dist) {
            //stub
        }

        private void Hit(Walker who) {
            //stub
        }
    }

    public interface AI {
        void update();
    }
    
    public interface HumanAI : AI {
    }

    public interface ZombieAI : AI {
    }
}
