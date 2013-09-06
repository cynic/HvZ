using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HvZCommon;

namespace HvZClient {
    public partial class JoinGameWindow : Window {
        private ObservableCollection<GameListItem> games = new ObservableCollection<GameListItem>();

        public JoinGameWindow(Game theGame) {
            InitializeComponent();

            games.Add(new GameListItem("test1", "000.000.000:00") {
                Description = "Blah bhal"
            });
            games.Add(new GameListItem("test2", "000.000.000:00") {
                Description = "Blah hahag",
                Unlocked = true
            });

            gamesList.ItemsSource = games;
        }

        public void HandleListPacket(string[] args) {
            if (args.Length > 1) {
                switch (args[0]) {
                    case "add": HandleRecievedListItem(args.Tail());
                        break;
                    case "close": HandleListItemClosed(args.Tail()[0]);
                        break;
                    case "lock": HandleListItemUnlocked(args.Tail());
                        break;
                }
            }
        }

        private void HandleRecievedListItem(string[] args) {
            if (args.Length > 1) {
                GameListItem item = new GameListItem(args[1], args[0]);
                if (args.Length > 1) {
                    if (args.Length > 2) {
                        item.Description = args[2];
                    }
                }

                int index = -1;
                if ((index = games.IndexOf(games.First(T => T.GameID == item.GameID))) >= 0) {
                    games[index] = item;
                } else {
                    games.Add(item);
                }
            }
        }

        private void HandleListItemClosed(string arg) {
            int index = -1;
            if ((index = games.IndexOf(games.First(T => T.GameID == arg))) >= 0) {
                games.RemoveAt(index);
            }
        }

        private void HandleListItemUnlocked(string[] args) {
            if (args.Length == 1) {
                int index = -1;
                if ((index = games.IndexOf(games.First(T => T.GameID == args[0]))) >= 0) {
                    bool unlocked = games[index].Unlocked;

                    Boolean.TryParse(args[1], out unlocked);

                    games[index].Unlocked = unlocked;
                }
            }
        }

        private void JoinGame(object sender, RoutedEventArgs e) {
            GameListItem item = ((GameListItem)((Button)sender).DataContext);
            Game.theGame.requestJoin(item.GameID);            
            GameWindow win = new GameWindow(item.Name);
            Game.theGame.OnGamestart += win.StartGame;
            win.Owner = this;
            win.Show();
        }

        private void CreateGame(object sender, RoutedEventArgs e) {
            //TODO: create game
        }

        private void Window_Closed(object sender, EventArgs e) {
            App.Current.Shutdown();
        }
    }
}
