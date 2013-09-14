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
        internal static AIType userAIType = AIType.DEFAULT;

        internal static ClientGame clientWorld = new ClientGame();

        public static string ClientID { get; private set; }
        public static string ServerID { get; private set; }

        internal static HvZConnection connection = new HvZConnection();

        public static void Start(HumanAI e) {
            userAIType = AIType.HUMAN;
            humanAIInstance = e;
            //StartProcesses();
        }

        public static void Start(ZombieAI e) {
            userAIType = AIType.ZOMBIE;
            zombieAIInstance = e;
            //StartProcesses();
        }

        public event GameStarted OnGamestart;
        public event HandlePacket OnListItemRecieved;
    }

    //event delegates
    public delegate void GameStarted();
    public delegate void HandlePacket(string[] args);
}
