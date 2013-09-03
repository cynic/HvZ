using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HvZCommon;

namespace HvZClient {
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class GameWindow : Window {
        private static readonly List<Key> pressedKeys = new List<Key>();

        public double RenderMultiplier { get; private set; }

        private DispatcherTimer ticker = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(300),
        };

        public GameWindow() {
            InitializeComponent();

            StartGame();
        }

        public void StartGame() {
            GUIMap.Visibility = Visibility.Visible;
            HideDialog();
            ticker.Tick += gameLoop;
            ticker.Start();
        }

        public void ShowDialog(string message) {
            Dialog_message.Content = message;
            Dialog_message.Visibility = Visibility.Visible;
        }

        public void HideDialog() {
            Dialog.Visibility = Visibility.Collapsed;
        }

        private void gameLoop(object sender, EventArgs e) {

        #region temporary test code
            //Hold down A to spawn zombies
            //Hold down A+S to spawn exploding zombies
            if (hasKeys(Key.A)) {
                Position pos = Utils.randPosition(Game.clientWorld.Map.Width, Game.clientWorld.Map.Height);
                string message = "S_spawnwalker_zombie_" + pos.X.ToString() + "_" + pos.Y.ToString() + "_20_0_0";
                if (hasKeys(Key.S)) {
                    message += "_1";
                }
                Game.theGame.HandleMessage(message);
            }
            //Hold down E to trigger special abilities
            if (hasKeys(Key.E)) {
                for (int i = 0; i < Game.clientWorld.Map.Children.Count; i++) {
                    Game.theGame.HandleMessage("S_special_" + i.ToString());
                }
            }
            //Hold down B to spawn humans
            if (hasKeys(Key.B)) {
                Position pos = Utils.randPosition(Game.clientWorld.Map.Width, Game.clientWorld.Map.Height);
                Game.theGame.HandleMessage("S_spawnwalker_human_" + pos.X.ToString() + "_" + pos.Y.ToString() + "_20_0_0");
            }
            //Hold down C to spawn supply points
            if (hasKeys(Key.C)) {
                Position pos = Utils.randPosition(Game.clientWorld.Map.Width, Game.clientWorld.Map.Height);
                Game.theGame.HandleMessage("S_spawnplace_" + pos.X.ToString() + "_" + pos.Y.ToString());
            }
            //Hold down shift to move things
            if (hasKeys(Key.LeftShift)) {
                foreach (ITakeSpace item in Game.clientWorld.Map.Children) {
                    if (item is IWalker) {
                        Game.Walk((IWalker)item, 1);
                    }
                }
            }
            //Hold down T to turn things
            if (hasKeys(Key.T)) {
                foreach (ITakeSpace item in Game.clientWorld.Map.Children) {
                    if (item is IWalker) {
                        Game.Turn((IWalker)item, 1);
                    }
                }
            }
        #endregion

            renderPass();
        }

        private void renderPass() {
            GUIMap.Children.Clear();
            Groupes things = Game.ThingsOnMap;
            renderItems(things.Obstacles);
            renderItems(things.SupplyPoints);
            renderItems(things.Zombies);
            renderItems(things.Humans);
            renderItems(things.Uncategorized);
        }

        private void renderItems(ITakeSpace[] items) {
            foreach (ITakeSpace i in items) {
                renderItem(i);
            }
        }

        private void renderItem(ITakeSpace item) {
            if (Game.clientWorld.Map.isInBounds(item)) {
                Image img = new Image() {
                    Source = Resource.getResourceByName(item.Texture).Image,
                    Width = RenderMultiplier * item.Radius * 2,
                    Height = RenderMultiplier * item.Radius * 2,
                };
                if (item is IWalker) {
                    img.RenderTransform = new RotateTransform(((IWalker)item).Heading) {
                        CenterX = img.Width / 2,
                        CenterY = img.Height / 2
                    };
                }
                
                Canvas.SetLeft(img, RenderMultiplier * (item.Position.X - item.Radius));
                Canvas.SetTop(img, RenderMultiplier * (item.Position.Y - item.Radius));
                GUIMap.Children.Add(img);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (!pressedKeys.Contains(e.Key)) {
                pressedKeys.Add(e.Key);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e) {
            if (pressedKeys.Contains(e.Key)) {
                pressedKeys.Remove(e.Key);
            }
        }

        public static bool hasKeys(params Key[] k) {
            foreach (Key i in k) {
                if (!pressedKeys.Contains(i)) {
                    return false;
                }
            }

            return true;
        }

        private void Window_Resized(object sender, SizeChangedEventArgs e) {
            double size = Math.Min(ActualWidth, ActualHeight);
            double mapSize = Math.Min(Game.clientWorld.Map.Width, Game.clientWorld.Map.Height);
            RenderMultiplier = size / mapSize;

            if (GUIMap.Width != Game.clientWorld.Map.Width * RenderMultiplier || GUIMap.Height != Game.clientWorld.Map.Height) {
                GUIMap.Width = Game.clientWorld.Map.Width * RenderMultiplier;
                GUIMap.Height = Game.clientWorld.Map.Height * RenderMultiplier;

                renderPass();
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            App.Current.Shutdown();
        }
    }
}
