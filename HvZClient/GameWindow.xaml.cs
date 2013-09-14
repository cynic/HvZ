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
using HvZ.Common;

namespace HvZClient {
    public enum CGState {
        Invalid,
        CreateRequested,
        CreationFailed,
        CreationSucceeded,
        JoinRequested,
        JoinFailed,
        JoinSucceeded
    }

    public enum Role {
        Invalid, Human, Zombie
    }
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class GameWindow : Window {
        private static readonly List<Key> pressedKeys = new List<Key>();
        ClientGame game;

        public double RenderMultiplier { get; private set; }

        private DispatcherTimer ticker = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(300),
        };

        public GameWindow(string name, string role, Map m) {
            InitializeComponent();
            game = new ClientGame(name, role, m);
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
            renderPass();
        }

        private void renderPass() {
            GUIMap.Children.Clear();
/*
            Groupes things = Game.ThingsOnMap;
            renderItems(things.Obstacles);
            renderItems(things.SupplyPoints);
            renderItems(things.Zombies);
            renderItems(things.Humans);
            renderItems(things.Uncategorized);
 */
        }

        private void renderItems(ITakeSpace[] items) {
            foreach (ITakeSpace i in items) {
                renderItem(i);
            }
        }

        private void renderItem(ITakeSpace item) {
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
            //Added null check so it doesnt crash. How do you get a ClientGame?
            if (Game.clientWorld != null) {
                double size = Math.Min(ActualWidth, ActualHeight);
/*
                double mapSize = Math.Min(Game.clientWorld.Map.Width, Game.clientWorld.Map.Height);
                RenderMultiplier = size / mapSize;

                if (GUIMap.Width != Game.clientWorld.Map.Width * RenderMultiplier || GUIMap.Height != Game.clientWorld.Map.Height) {
                    GUIMap.Width = Game.clientWorld.Map.Width * RenderMultiplier;
                    GUIMap.Height = Game.clientWorld.Map.Height * RenderMultiplier;

                    renderPass();
                }
 */
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            var messages = new[] {
                "I wouldn't leave if I were you.  Real work is much worse.",
                "You're trying to say you like TV better than me, right?",
                "Don't leave - there's food around that corner!",
                "You know, next time you come in here, I'm going to get you.",
                "Go ahead and leave.  See if I care.",
                "Get out of here and go back to your boring programs.",
                "Look, bud.  You leave now and you forfeit your body count.",
                "Just leave.  When you come back, I'll be waiting with a bat.",
                "You're lucky I don't smack you for thinking about leaving.",
                "Don't leave now - there's a dimensional shambler waiting for you in Windows!"
            };
            if (MessageBox.Show(messages[DateTime.Now.Second%messages.Length], "Leave Game", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) {
                //Game.theGame.EndGame();
                Owner.Focus();
            } else {
                e.Cancel = true;
            }
        }
    }
}
