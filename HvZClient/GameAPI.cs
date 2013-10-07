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
        private Map gameMap;
        private int slots_left;
        private List<IZombieAI> zAI = new List<IZombieAI>();
        private List<IHumanAI> hAI = new List<IHumanAI>();

        /// <summary>
        /// Called to create a new game with the given name and map
        /// </summary>
        /// <param name="name">A unique name for this game</param>
        /// <param name="map">The map to play this game on</param>
        public Game(string name, Map map) {
            gameName = name;
            gameMap = map;
            slots_left = gameMap.spawners.Count;
        }

        public void Start() {
            // yes, I'm aware of how horrible this is.
            //Horrible does not even begin to describe this. It also did not work.
            GameWindow g = new GameWindow(new ClientGame(gameName, gameMap));
            g.Title = gameName;
            g.Show();
            foreach (IZombieAI z in zAI) g.game.AddZombie(z);
            foreach (IHumanAI h in hAI) g.game.AddHuman(h);
        }

        public int Slots { get { return slots_left; } }

        public void CloseSlot() {
            if (slots_left == 0) return;
            gameMap.CloseSlot();
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
