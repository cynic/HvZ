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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HvZ.Common;

namespace HvZ.Client {
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    partial class GameWindow : Window {
        internal ClientGame game;
        // WPF uses retained-mode graphics, not immediate-mode.  See http://msdn.microsoft.com/en-us/library/ms748373.aspx#visual_rendering_behavior .  No need to simulate immediate-mode.
        // A better plan might be to do most of this stuff in XAML, using DataTemplates http://msdn.microsoft.com/en-us/library/ms742521.aspx
        // Let's see if I can get away with implementing INotifyPropertyChanged.  Necessary, but such boilerplate in C#...

        string MakeScoreboard() {
            var sb = new StringBuilder();
            foreach (var kvp in game.scoreboard) {
                if (game.Map.walkers.ContainsKey(kvp.Key)) {
                    sb.AppendFormat("{0} - still alive!\n", kvp.Value.Item1);
                } else {
                    sb.AppendFormat("{0} - survived for {1:F2} seconds\n", kvp.Value.Item1, TimeSpan.FromTicks(kvp.Value.Item2).TotalSeconds);
                }
            }
            return sb.ToString();
        }

        public GameWindow() {
            InitializeComponent();
            Title = (string)Application.Current.Resources["gameName"];
            game = (ClientGame)Application.Current.Resources["clientGame"];
            game.OnMapChange += (_, __) => PlaceObjects(GUIMap, game.Map);
            game.HumansWin += (_, __) => MessageBox.Show("Humans have survived, and all zombies are dead.  Humans win!\n" + MakeScoreboard(), "Victory for Humans!");
            game.ZombiesWin += (_, __) => MessageBox.Show("Zombies have killed all the humans.  Zombies win!\n" + MakeScoreboard(), "Victory for Zombies!");
            game.Draw += (_, __) => MessageBox.Show("Tragically, neither zombies nor humans survived.\n" + MakeScoreboard(), "It's a draw.");
        }

        private static void placeMissile(IDirectedVisual missile, Canvas c) {
            var e = new Ellipse() { Width = missile.Radius * 2 + 0.2, Height = missile.Radius * 2 + 0.2 };
            e.Fill = (Brush)Application.Current.Resources[missile.Texture];
            //e.Stroke = Brushes.;
            //e.StrokeThickness = 0.1;
            var group = new TransformGroup();
            var translate = new TranslateTransform(missile.Position.X - missile.Radius, missile.Position.Y - missile.Radius);
            var rotate = new RotateTransform(missile.Heading, missile.Radius, missile.Radius);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            e.RenderTransform = group;
            ((System.ComponentModel.INotifyPropertyChanged)missile.Position).PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case "X":
                        translate.X = missile.Position.X - missile.Radius;
                        break;
                    case "Y":
                        translate.Y = missile.Position.Y - missile.Radius;
                        break;
                    default:
                        throw new Exception("Unhandled property name from Position");
                }
            };
            c.Children.Add(e);
        }

        private static void placeWalker(string texture, IWalkerExtended walker, Canvas c) {
            var e = new Ellipse() { Width = walker.Radius*2, Height = walker.Radius*2 };
            e.Fill = (ImageBrush)Application.Current.Resources[texture];
            //e.Stroke = Brushes.Gray;
            e.StrokeThickness = 0.2;
            // this is the direction arc, which shows which way the player is facing.
            var frontStrokeThickness = 0.25;
            var dirpath = new Path() { Stroke = Brushes.HotPink, StrokeThickness = frontStrokeThickness, StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round, Opacity=0.6 };
            var dirfigure = new PathFigure() { IsClosed = false, StartPoint = new Point(-walker.Radius*Math.Sin((55.0).ToRadians()) + walker.Radius, walker.Radius*Math.Cos((55.0).ToRadians())) };
            var dirarc = new ArcSegment() {
                SweepDirection = System.Windows.Media.SweepDirection.Clockwise,
                Point = new Point(walker.Radius * Math.Sin((55.0).ToRadians()) + walker.Radius, walker.Radius * Math.Cos((55.0).ToRadians())),
                IsStroked = true,
                Size = new Size(walker.Radius, walker.Radius)
            };
            var dirgeom = new PathGeometry(new[] { dirfigure });
            dirfigure.Segments.Add(dirarc);
            dirpath.Data = dirgeom;
            // here is the remaining lifespan.
            var green = new GradientStop(Colors.Green, 1.0-0.01);
            var red = new GradientStop(Colors.Red, 1.0);
            var lifebar = new LinearGradientBrush(new GradientStopCollection(new[] { new GradientStop(Colors.Green, 0.0), green, red }));
            var liferec = new Rectangle() { Width = walker.Radius * 2, RadiusX = 0.25, RadiusY = 0.25, Height = 0.3, Fill = lifebar, Margin = new Thickness(0.0, walker.Radius * 2, 0.0, 0.0), Opacity = 0.6 };
            //var arc = new HvZ.Controls.Arc() { Center = new Point(walker.Radius, walker.Radius), Stroke = Brushes.Blue, StartAngle = 45, EndAngle = 315, StrokeThickness = 0.4, SmallAngle = false, Opacity=0.6, Radius = walker.Radius-0.3 };
            var group = new TransformGroup();
            var translate = new TranslateTransform(walker.Position.X - walker.Radius, walker.Position.Y - walker.Radius);
            var rotate = new RotateTransform(walker.Heading, walker.Radius, walker.Radius);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            e.RenderTransform = group;
            dirpath.RenderTransform = group;
            liferec.RenderTransform = translate;
            ((System.ComponentModel.INotifyPropertyChanged)walker.Position).PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case "X":
                        translate.X = walker.Position.X - walker.Radius;
                        break;
                    case "Y":
                        translate.Y = walker.Position.Y - walker.Radius;
                        break;
                    default: throw new Exception("Unhandled property name from Position");
                }
            };
            walker.PropertyChanged += (_, args) => {
                switch (args.PropertyName) {
                    case "Heading":
                        rotate.Angle = walker.Heading;
                        break;
                    case "Lifespan":
                        var amount = (double)walker.Lifespan / (double)walker.MaximumLifespan;
                        green.Offset = amount-0.01;
                        red.Offset = amount;
                        break;
                    case "StunRemaining":
                        if (((Zombie)walker).IsStunned) {
                            e.Stroke = Brushes.Yellow;
                        } else {
                            e.Stroke = null;
                        }
                        break;
                    default: throw new Exception("Unhandled property name from Walker");
                }
                
            };
            c.Children.Add(e);
            c.Children.Add(dirpath);
            c.Children.Add(liferec);
        }

        private static void placeObstacle(IVisual item, Canvas c) {
            var e = new Ellipse() { Width = item.Radius * 2, Height = item.Radius * 2 };
            e.Fill = (ImageBrush)Application.Current.Resources[item.Texture];
            //e.Opacity = 0.65;
            if (item.Radius <= 0.45) {
                e.Stroke = Brushes.Black;
                e.StrokeThickness = 0.1;
            }
            var translate = new TranslateTransform(item.Position.X - item.Radius, item.Position.Y - item.Radius);
            e.RenderTransform = translate;
            c.Children.Add(e);
        }

        private static void placeResupply(ResupplyPoint item, Canvas c) {
            var e = new Ellipse() { Width = item.Radius * 2, Height = item.Radius * 2 };
            e.Fill = (ImageBrush)Application.Current.Resources["supply"];
            e.Stroke = Brushes.Black;
            e.StrokeThickness = 0.1;
            var translate = new TranslateTransform(item.Position.X - item.Radius, item.Position.Y - item.Radius);
            e.RenderTransform = translate;
            c.Children.Add(e);
        }

        internal static void PlaceObjects(Canvas c, Map m) {
            c.Children.Clear();
            c.RenderTransform = new ScaleTransform(c.ActualWidth / m.Width, c.ActualHeight / m.Height);
            foreach (var o in m.Obstacles.OrderBy(x => x.Radius)) placeObstacle(o, c);
            foreach (var s in m.ResupplyPoints) placeResupply(s, c);
            foreach (var h in m.Humans) placeWalker("human", h, c);
            foreach (var z in m.Zombies) placeWalker("zombie", z, c);
            foreach (var mx in m.missiles) placeMissile(mx, c);
        }

        private void Window_Resized(object sender, SizeChangedEventArgs e) {
            GUIMap.RenderTransform = new ScaleTransform(GUIMap.ActualWidth / game.Width, GUIMap.ActualHeight / game.Height);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ((IDisposable)game).Dispose();
            Application.Current.Shutdown(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            PlaceObjects(GUIMap, game.Map);
        }
    }
}
