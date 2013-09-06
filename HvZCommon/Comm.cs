using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HvZCommon {
    public class GameListItem {
        public string Name { get; private set; }
        public string GameID { get; private set; }

        public string Description { get; set; }

        public bool Unlocked { get; set; }

        public GameListItem(string name, string id) {
            Name = name;
            GameID = id;
        }
    }
}
