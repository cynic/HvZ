using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HvZ.Common;

namespace HvZClient {
    public class Game {
        internal static ZombieAI zombieAIInstance = new ZombieAI();
        internal static HumanAI humanAIInstance = new HumanAI();

        internal static ClientGame clientWorld;
        internal static HvZConnection connection = new HvZConnection();

    }

    //event delegates
    public delegate void GameStarted();
    public delegate void HandlePacket(string[] args);
}
