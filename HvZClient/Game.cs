using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HvZCommon;

namespace HvZClient {
    public class Game {
        internal static ZombieAI zombieAIInstance = new ZombieAI();
        internal static HumanAI humanAIInstance = new HumanAI();
        internal static AIType userAIType = AIType.DEFAULT;

        internal static Game theGame;

        public static string ClientID { get; private set; }

        internal static GameState clientWorld = new GameState();

        public static void Start(HumanAI e) {
            userAIType = AIType.HUMAN;
            humanAIInstance = e;
            StartProcesses();
        }

        public static void Start(ZombieAI e) {
            userAIType = AIType.ZOMBIE;
            zombieAIInstance = e;
            StartProcesses();
        }

        internal static void StartProcesses() {
            theGame = new Game();
            theGame.requestJoin();

            GameWindow window = new GameWindow();
            theGame.OnGamestart += window.StartGame;
            
            window.Show();
        }

        internal static void SendMessage(string pack, params object[] arguments) {
            //stub
        }

        internal void HandleMessage(string pack) {
            //stub
            string[] data = pack.Split('_');

            if (data.Length > 1) {
                string[] argument = data.Tail();
                switch (data[0]) {
                    case "S": Debug.Print("the server is talking to me!");
                        HandleServerMessage(argument);
                        break;
                    case "A": Debug.Print("the server wants me to do something");
                        HandleActionMessage(argument);
                        break;
                    case "C": Debug.Print("a client is talking to me, wtf?");
                        break;
                    default: Debug.Print("i don't know what to do with this: " + pack);
                        break;
                }
            }
        }

        private void HandleServerMessage(string[] args) {
            switch (args[0]) {
                case "spawnwalker": SpawnWalker(args.Tail());
                    break;
                case "spawnplace": SpawnPlace(args.Tail());
                    break;
                case "refresh": RefreshPositions(args.Tail());
                    break;
                case "load": LoadWorld(args.Tail());
                    break;
                default: Debug.Print(args[0] + " is not a recognized command.");
                    break;
            }
        }

        private void LoadWorld(string[] args) {
            clientWorld.Dirty = true;
            SendMessage("C_connect", "reload");
        }

        private void RefreshPositions(string[] args) {
            if (clientWorld.Map.Children.Count != args.Length * 3) {
                SendMessage("C_failed", "refresh");
            }

            int index = 0;

            foreach (ITakeSpace i in clientWorld.Map.Children) {
                Position oldPos = i.Position;
                if (!i.Position.newPosition(args[index + 1], args[index + 2])) {
                    SendMessage("C_failed", "refresh");
                    return;
                }
                if (i is IWalker) {
                    OnWalk((IWalker)i, oldPos.distanceFrom(i.Position));
                }
                index += 3;
            }

            SendMessage("C_success", "refresh");
        }

        private void SpawnPlace(string[] data) {
            if (data.Length >= 3) {
                double posX = 0;
                double posY = 0;

                Double.TryParse(data[1], out posX);
                Double.TryParse(data[2], out posY);

                clientWorld.Spawn(new ResupplyPoint() { Position = new Position(posX, posY) });
                SendMessage("C_success", "spawnplace");
            }
        }

        private void SpawnWalker(string[] data) {
            if (data.Length >= 3) {
                IWalker item = null;

                double posX = 0;
                double posY = 0;
                double head = 0;
                double speed = 0;

                Double.TryParse(data[1], out posX);
                Double.TryParse(data[2], out posY);
                if (data.Length >= 5) {
                    Double.TryParse(data[3], out head);
                    Double.TryParse(data[4], out speed);
                }

                if (data[0] == "zombie") {
                    item = new Zombie();
                } else if (data[0] == "human") {
                    item = new Human();
                }
                item.Position = new Position(posX, posY);

                if (item != null) {
                    item.Heading = head;
                    item.Speed = speed;
                    clientWorld.Spawn(item);
                    SendMessage("C_success", "spawnwalker");
                    OnSpawn(item);
                }
            }
        }

        private void HandleActionMessage(string[] args) {
            switch (args[0]) {
                //stub
                //SendMessage("C_success", args[0]);
                default: Debug.Print(args[0] + " is not a recognized action.");
                    SendMessage("C_failed", args[0]);
                    break;
            }
        }

        internal void requestJoin() {
            SendMessage("C_hello");
        }

        private void JoinGame(string id) {
            ClientID = id;
            OnGamestart();
        }

        private void EndGame() {
            SendMessage("C_quit", ClientID);
        }

        public static void Throw(IWalker who) {
            SendMessage("A_throw", who);
        }

        public static void Walk(IWalker who, double dist) {
            SendMessage("A_walk", who, dist);
        }

        private void Hit(IWalker who, IWalker attacker) {
            SendMessage("A_hit", who, attacker);
        }

        public static void Turn(IWalker who, double degrees) {
            SendMessage("A_turn", who, degrees);
        }

        public static void Eat(IWalker who, IWalker victim) {
            SendMessage("A_eat", who, victim);
        }

        public static void Take(IWalker who, ITakeSpace where) {
            SendMessage("A_take", who, where);
        }

        public static void Consume(Human who) {
            SendMessage("A_consume", who);
        }

        public static Groupes ThingsOnMap {
            get {
                return new Groupes(clientWorld.Map.Children);
            }
        }

        public event GameStarted OnGamestart;
        public event Hit OnHit;
        public event Hungering OnHungry;
        public event Spawned OnSpawn;
        public event Walk OnWalk;
    }

    //event delegates
    public delegate void GameStarted();
    public delegate void Spawned(IWalker me);
    public delegate void Hungering(IWalker me);
    public delegate void Hit(IWalker me, IWalker attacker);
    public delegate void Walk(IWalker me, double distance);
}
