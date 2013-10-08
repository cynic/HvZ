using System.Windows;

namespace HvZ.Client {
    partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            Game game = new Game("woo", new Map("Maps/Another Map.txt"));
            for (int i = 0; i < Extensions.rand.Next(5, 15); i++) game.AddHuman(new AI.GreedyHuman());
            game.Start();
        }
    }
}
