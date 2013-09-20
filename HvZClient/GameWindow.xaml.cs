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
        ClientGame game;

        public double RenderMultiplier { get; private set; }
        DispatcherTimer ticker = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(100)
        };

        public GameWindow(string name, string role, Map m, HvZ.AI.IZombieAI ai) {
            InitializeComponent();
            Title = name + " - " + role;
            game = new ClientGame(name, role, m, ai);
            GUIMap.Background = ClientWindow.ImageFromMap(m);
            StartGame();
        }

        public GameWindow(string name, string role, Map m, HvZ.AI.IHumanAI ai) {
            InitializeComponent();
            Title = name + " - " + role;
            game = new ClientGame(name, role, m, ai);
            GUIMap.Background = ClientWindow.ImageFromMap(m);
            StartGame();
        }

        public void StartGame() {
            GUIMap.Visibility = Visibility.Visible;
            //HideDialog();
            ticker.Tick += (o, e) => gameLoop();
            ticker.Start();
        }

        /* // "Dialog" doesn't exist, near as I can tell.  Uncommitted stuff?
        public void ShowDialog(string message) {
            Dialog_message.Content = message;
            Dialog_message.Visibility = Visibility.Visible;
        }

        public void HideDialog() {
            Dialog.Visibility = Visibility.Collapsed;
        }
         */

        private void gameLoop() {
            renderPass();
        }

        private void renderPass() {
            GUIMap.Children.Clear();

            Groupes things = game.MapContents;
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

        private void Window_Resized(object sender, SizeChangedEventArgs e) {
            if (game != null) {

                double multX = top.ActualWidth / game.Width;
                double multY = top.ActualHeight / game.Height;

                RenderMultiplier = Math.Min(multX, multY);

                if (GUIMap.ActualWidth != (game.Width * RenderMultiplier) || GUIMap.ActualHeight != (game.Height * RenderMultiplier)) {
                    GUIMap.Width = (game.Width * RenderMultiplier);
                    GUIMap.Height = (game.Height * RenderMultiplier);

                    renderPass();
                }
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
            if (MessageBox.Show(messages.PickNext(), "Leave Game", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) {
                //Game.theGame.EndGame();
                Owner.Focus();
            } else {
                e.Cancel = true;
            }
        }
    }
}
