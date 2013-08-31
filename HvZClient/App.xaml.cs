using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace HvZClient {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public Game theGame;

        protected override void OnStartup(StartupEventArgs e) {
            if (!e.Args.Contains(Game.STARTUP_ARG)) {
                ClientWindow win = new ClientWindow();
                win.Show();


                Game.StartProcesses();
                Shutdown();
            }

            base.OnStartup(e);
            theGame = new Game();

            GameWindow window = new GameWindow();
            window.Show();
        }

        protected override void OnLoadCompleted(NavigationEventArgs e) {
            base.OnLoadCompleted(e);
            theGame.requestJoin();
            theGame.OnGamestart += ((GameWindow)MainWindow).StartGame;
        }
    }
}
