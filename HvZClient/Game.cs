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
        public static string ServerID { get; private set; }

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
            JoinGameWindow window = new JoinGameWindow(theGame);
            theGame.OnListItemRecieved += window.HandleListPacket;
            window.Show();
        }

        internal static void SendMessage(string pack, params object[] arguments) {
            foreach (object i in arguments) {
                if (i is int || i is double) {
                    pack += "_" + i.ToString();
                } else if (i is ITakeSpace) {
                    pack += "_" + clientWorld.Map.Children.IndexOf((ITakeSpace)i).ToString();
                }
            }

            DoSend(pack);
        }

        private static void DoSend(string pack) {
            //sends message to server located at ServerID


            //stub
            //test code
            if (pack.StartsWith("C_success")) {
                Debug.Print("YAY");
            } else if (pack.StartsWith("C_failed")) {
                Debug.Print("AWW");
            } else {
                Debug.Print("i think i'm talking to the server! XP");
            }

            if (pack.StartsWith("A_turn")) {
                int index = Int32.Parse(pack.Split('_')[2]);
                int deg = Int32.Parse(pack.Split('_')[3]);

                string message = "S_refresh_pos";
                for (int i = 0; i < clientWorld.Map.Children.Count; i++) {
                    ITakeSpace item = clientWorld.Map.Children[i];
                    double head = 0;
                    if (item is IWalker) {
                        head = ((IWalker)item).Heading + deg;
                    }

                    message += String.Format("_{0}_{1}_{2}", item.Position.X.ToString(), item.Position.Y, head);
                }

                theGame.HandleMessage(message);
            } else if (pack.StartsWith("A_walk")) {
                string message = "S_refresh_pos";

                int index = Int32.Parse(pack.Split('_')[2]);
                int dist = Int32.Parse(pack.Split('_')[3]);


                for (int i = 0; i < clientWorld.Map.Children.Count; i++) {
                    ITakeSpace item = clientWorld.Map.Children[i];
                    double head = 0;
                    if (item is IWalker) {
                        head = ((IWalker)item).Heading;
                    }

                    message += String.Format("_{0}_{1}_{2}", (item.Position.X + (i == index ? dist : 0)).ToString(), item.Position.Y, head);
                }

                theGame.HandleMessage(message);
            }
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
                case "refresh": Refresh(args.Tail());
                    break;
                case "load": LoadWorld(args.Tail());
                    break;
                case "hit": HandleHit(args.Tail());
                    break;
                case "hungry": HandleHungry(args.Tail());
                    break;
                case "special": HandleSpecial(args.Tail());
                    break;
                case "list": OnListItemRecieved(args.Tail());
                    break;
                default: Debug.Print(args[0] + " is not a recognized command.");
                    break;
            }
        }

        private void LoadWorld(string[] args) {
            clientWorld.Dirty = true;
            SendMessage("C_connect", "reload");
        }

        private void Refresh(string[] args) {
            switch (args[0]) {
                case "pos": RefreshPos(args.Tail());
                    break;
                case "heal": RefreshHeal(args.Tail());
                    break;
            }
        }

        private void RefreshHeal(string[] args) {
            if (clientWorld.Map.Children.Count != args.Length) {
                SendMessage("C_failed", "refresh");
                return;
            }

            int index = 0;
            foreach (ITakeSpace i in clientWorld.Map.Children) {
                if (i is IWalker) {
                    int health = -1;
                    Int32.TryParse(args[index], out health);
                    if (health >= 0) {
                        ((IWalker)i).Health = health;
                    }
                }
                index++;
            }
        }

        private void RefreshPos(string[] args) {
            if (clientWorld.Map.Children.Count * 3 != args.Length) {
                SendMessage("C_failed", "refresh");
                return;
            }

            int index = 0;

            foreach (ITakeSpace i in clientWorld.Map.Children) {
                Position oldPos = i.Position;
                if (!i.Position.newPosition(args[index], args[index + 1])) {
                    SendMessage("C_failed", "refresh");
                    return;
                }

                if (i is IWalker) {
                    double heading = ((IWalker)i).Heading;
                    Double.TryParse(args[index + 2], out heading);
                    ((IWalker)i).Heading = heading;
                }

                double dist = oldPos.distanceFrom(i.Position);
                if (i is IWalker && (dist > 0 || dist < 0)) {
                    specificAI((IWalker)i).Walked((IWalker)i, dist);
                }
                index += 3;
            }

            SendMessage("C_success", "refresh");
        }

        private void SpawnPlace(string[] data) {
            if (data.Length >= 2) {
                double posX = 0;
                double posY = 0;
                double rad = 70;

                Double.TryParse(data[0], out posX);
                Double.TryParse(data[1], out posY);
                if (data.Length >= 3) {
                    Double.TryParse(data[2], out rad);
                }

                clientWorld.Spawn(new ResupplyPoint() { Position = new Position(posX, posY), Radius = rad });
                SendMessage("C_success", "spawnplace");
            }
        }

        private void SpawnWalker(string[] data) {
            if (data.Length >= 4) {
                IWalker item = null;

                double posX = 0;
                double posY = 0;
                int health = -1;
                double head = 0;
                double speed = 0;

                Double.TryParse(data[1], out posX);
                Double.TryParse(data[2], out posY);
                Int32.TryParse(data[3], out health);
                if (data.Length >= 6) {
                    Double.TryParse(data[4], out head);
                    Double.TryParse(data[5], out speed);
                }

                if (data[0] == "zombie") {
                    if (data.Length >= 7) {
                        int type = 0;
                        Int32.TryParse(data[6], out type);
                        item = new Zombie(health, type, Blowup);
                    } else {
                        item = new Zombie(health);
                    }
                } else if (data[0] == "human") {
                    item = new Human(health);
                }
                item.Position = new Position(posX, posY);

                if (item != null) {
                    item.Heading = head;
                    item.Speed = speed;
                    item.Radius = 15;
                    clientWorld.Spawn(item);
                    SendMessage("C_success", "spawnwalker");
                    specificAI(item).Spawned(item);
                }
            }
        }

        private void HandleSpecial(string[] args) {
            if (args.Length == 1) {
                int index = -1;

                Int32.TryParse(args[0], out index);

                ITakeSpace walki = clientWorld.Map.ElementAt(index);

                if (walki != null && walki is IWalker) {
                    ((IWalker)walki).TriggerSpecial();
                }
            }
        }

        private void Blowup(IWalker me) {
            Groupes groups = ThingsOnMap;

            double explosionRadius = 60;

            clientWorld.Spawn(new ExplosionEffect(me, explosionRadius));

            foreach (IWalker i in groups.Humans) {
                if (me.Position.distanceFrom(i.Position) < explosionRadius) {
                    Throw(i);
                }
            }

            foreach (IWalker i in groups.Zombies) {
                if (me.Position.distanceFrom(i.Position) < explosionRadius) {
                    Throw(i);
                }
            }
        }

        private void HandleHit(string[] args) {
            if (args.Length == 2) {
                int hitten = -1;
                int hitter = -1;

                Int32.TryParse(args[0], out hitten);
                Int32.TryParse(args[1], out hitter);

                ITakeSpace who = clientWorld.Map.ElementAt(hitten);
                ITakeSpace attacker = clientWorld.Map.ElementAt(hitter);

                if (who != null && attacker != null) {
                    if (who is IWalker && attacker is IWalker) {
                        specificAI((IWalker)who).Hit((IWalker)who, (IWalker)attacker);
                    }
                }
            }
        }

        private void HandleHungry(string[] args) {
            if (args.Length == 1) {
                int walker = -1;

                Int32.TryParse(args[0], out walker);

                ITakeSpace hungerer = clientWorld.Map.ElementAt(walker);
                if (hungerer != null && hungerer is IWalker) {
                    specificAI((IWalker)hungerer).Hungering((IWalker)hungerer);
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

        internal void requestJoin(string ip) {
            ServerID = ip;
            SendMessage("C_hello");
        }

        private void JoinGame(string id) {
            ClientID = id;
            OnGamestart();
        }

        public void EndGame() {
            SendMessage("C_quit", ClientID);
        }

        public static void Throw(IWalker who) {
            SendMessage("A_throw", who);
        }

        public static void Walk(IWalker who, double dist) {
            SendMessage("A_walk", who, dist);
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

        internal static BaseAI specificAI(IWalker item) {
            if (item is Zombie) {
                return zombieAIInstance;
            }
            return humanAIInstance;
        }

        public event GameStarted OnGamestart;
        public event HandlePacket OnListItemRecieved;
    }

    //event delegates
    public delegate void GameStarted();
    public delegate void HandlePacket(string[] args);
}
