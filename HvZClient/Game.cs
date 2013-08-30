using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HvZCommon;

namespace HvZClient {
    public class Game {
        internal static AI userAIInstance;
        internal static AIType userAIType;

        internal static GameState clientWorld = new GameState();

        public static void Start(HumanAI e) {
            userAIType = AIType.HUMAN;
            StartProcesses(e);
        }

        public static void Start(ZombieAI e) {
            userAIType = AIType.ZOMBIE;
            StartProcesses(e);
        }

        internal static void StartProcesses(AI e) {
            userAIInstance = e;
            //stub
        }

        internal static void SendMessage(string pack, params object[] arguments) {
            //stub
        }

        internal void HandleMessage(string pack) {
            //stub
            string[] data = pack.Split('_');

            if (data.Length > 1) {
                string[] argument = data.Skip(1).ToArray();
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
                case "spawnwalker": SpawnWalker(args.Skip(1).ToArray());
                    break;
                case "spawnplace": SpawnPlace(args.Skip(1).ToArray());
                    break;
                case "refresh": RefreshPositions(args.Skip(1).ToArray());
                    break;
                case "load": LoadWorld(args.Skip(1).ToArray());
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
                if (!i.Position.newPosition(args[index + 1], args[index + 2])) {
                    SendMessage("C_failed", "refresh");
                    return;
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

        private void EndGame() {
            //stub
        }

        public static void Throw(IWalker who) {
            //stub
        }

        public static void Walk(IWalker who, double dist) {
            //stub
        }

        private void Hit(IWalker who, IWalker attacker) {
            //stub
        }

        public static void Turn(IWalker who, double degrees) {
            //stub
        }

        public static void Eat(IWalker who, IWalker victim) {
            //stub
        }

        public static void Take(IWalker who, ITakeSpace where) {
            //stub
        }

        public static void Consume(Human who) {
            //stub
        }

        public static Groupes ThingsOnMap {
            get {
                //stub
                throw new NotImplementedException();
            }
        }
    }

    //event delegates
    public delegate void Spawned(IWalker me);
    public delegate void Hungering(IWalker me);
    public delegate void Hit(IWalker me, IWalker attacker);
    public delegate void Walk(IWalker me, double distance);

    public interface AI {
        event Spawned OnSpawned;
        event Hungering OnHungry;
        event Killed OnKilled;
        event Hit OnHit;
        event Walk OnWalking;
    }

    public enum AIType {
        ZOMBIE, HUMAN
    }
    
    public interface HumanAI : AI {
        void update(Human me);
    }

    public interface ZombieAI : AI {
        void update(Zombie me);
    }
}
