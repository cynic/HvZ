﻿using System;
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
using HvZ.AI;
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

        private GameWindow(string name, string role, Map m) {
            InitializeComponent();
            Title = name + " - " + role;
            GUIMap.Background = ClientWindow.ImageFromMap(m);
            StartGame();
        }

        public GameWindow(string name, string role, Map m, IZombieAI ai) : this(name, role, m) {
            game = new ClientGame(name, role, m, ai);
        }

        public GameWindow(string name, string role, Map m, IHumanAI ai) : this(name, role, m) {
            game = new ClientGame(name, role, m, ai);
        }

        public void StartGame() {
            ticker.Tick += (o, e) => gameLoop();
            ticker.Start();
        }

        private void gameLoop() {
            renderPass();
        }

        private void renderPass() {
            GUIMap.Children.Clear();
            Groups things = game.MapContents;
            renderItems(things.Obstacles);
            renderItems(things.SupplyPoints);
            renderItems(things.Zombies);
            renderItems(things.Humans);
        }

        private void renderItems(ITakeSpace[] items) {
            foreach (ITakeSpace i in items) {
                if (game.isInBounds(i)) {
                    renderItem(i);
                }
            }
        }

        private void renderItem(ITakeSpace item) {
            Ellipse e = new Ellipse() { Width = item.Radius * RenderMultiplier * 2, Height = item.Radius * RenderMultiplier * 2 };
            e.Fill = (ImageBrush)Resources[item.Texture];
            if (item is IWalker) {
                e.RenderTransform = new RotateTransform(((IWalker)item).Heading) {
                    CenterX = item.Radius * RenderMultiplier,
                    CenterY = item.Radius * RenderMultiplier
                };
            }                
            Canvas.SetLeft(e, RenderMultiplier * (item.Position.X - item.Radius));
            Canvas.SetTop(e, RenderMultiplier * (item.Position.Y - item.Radius));
            GUIMap.Children.Add(e);
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
