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
            //Hold down space to spawn zombies
            if (hasKeys(Key.Space)) {
                Game.clientWorld.Spawn(new Zombie() {
                    Radius = 15,
                });
            }
        #endregion

            renderPass();
        }

        private void renderPass() {
            GUIMap.Children.Clear();

            foreach (ITakeSpace i in Game.clientWorld.Map.Children) {
                if (Game.clientWorld.Map.isInBounds(i)) {
                    Image img = new Image() {
                        Source = Resource.getResourceByName(i.Texture).Image,
                        Width = RenderMultiplier * i.Radius * 2,
                        Height = RenderMultiplier * i.Radius * 2,
                    };
                    if (i is IWalker) {
                        img.RenderTransform = new RotateTransform(((IWalker)i).Heading);
                    }
                    Canvas.SetLeft(img, RenderMultiplier * (i.Position.X - i.Radius));
                    Canvas.SetTop(img, RenderMultiplier * (i.Position.Y - i.Radius));
                    GUIMap.Children.Add(img);
                }
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
    }
}
