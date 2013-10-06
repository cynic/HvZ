using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ.AI;
using HvZ.Client;
using HvZ.Common;

namespace HvZ {
    public class Game {
        private string gameName;
        private Map m;
        private int slots_left;
        private List<IZombieAI> zAI = new List<IZombieAI>();
        private List<IHumanAI> hAI = new List<IHumanAI>();

        /// <summary>
        /// Called to create a new game with the given name and map
        /// </summary>
        /// <param name="gameName">A unique name for this game</param>
        /// <param name="map">The map to play this game on</param>
        public Game(string gameName, Map map) {
            this.gameName = gameName;
            m = map;
            slots_left = m.spawners.Count;
        }

        public void Start() {
            var app = new App();
            app.InitializeComponent();
            // yes, I'm aware of how horrible this is.
            app.Resources["gameName"] = gameName;
            app.Resources["clientGame"] = new ClientGame(gameName, m);
            GameWindow g = new GameWindow(); // ... which uses the Resources I've just set up...
            foreach (var z in zAI) g.game.AddZombie(z);
            foreach (var h in hAI) g.game.AddHuman(h);
            app.Run();
        }

        public int Slots { get { return slots_left; } }

        public void CloseSlot() {
            if (slots_left == 0) return;
            m.CloseSlot();
            slots_left--;
        }

        public bool AddZombie(IZombieAI zombieAI) {
            if (slots_left == 0) return false;
            zAI.Add(zombieAI);
            slots_left--;
            return true;
        }

        public bool AddHuman(IHumanAI humanAI) {
            if (slots_left == 0) return false;
            hAI.Add(humanAI);
            slots_left--;
            return true;
        }
    }
}
