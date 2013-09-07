using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZNetworking;

namespace HvZCommon {
    /* Constructors:
     * The client gets to start a game or join an existing game.  In the case
     * of starting a game, the client can choose parameters.  In the case of
     * joining a game, the client has no say.
     */
    public class ClientGame {
        private HvZConnection connection = new HvZConnection();

        private ClientGame(int width, int height) {
            Map = new Map(width, height);
            connection.OnCommandReceived += (o,args) => OnGameCommand(this, args);
            connection.ConnectToServer("localhost");
            connection.Send(Command.NewCreate((uint)width, (uint)height));
            GameTime = 0;
        }

        private ClientGame(string gameId) {
            connection = new HvZConnection();
            connection.Send(Command.NewJoin(gameId));
        }

        public event EventHandler<CommandEventArgs> OnGameCommand;

        public Map Map { get; set; }
        public UInt32 GameTime { get; set; }
    }
}
