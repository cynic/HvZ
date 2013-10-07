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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HvZ.AI;
using HvZ.Common;

namespace HvZ.Client {
    enum Role {
        Invalid, Human, Zombie
    }
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    partial class GameWindow : Window {
        internal ClientGame game;
        // WPF uses retained-mode graphics, not immediate-mode.  See http://msdn.microsoft.com/en-us/library/ms748373.aspx#visual_rendering_behavior .  No need to simulate immediate-mode.
        // A better plan might be to do most of this stuff in XAML, using DataTemplates http://msdn.microsoft.com/en-us/library/ms742521.aspx
        // Let's see if I can get away with implementing INotifyPropertyChanged.  Necessary, but such boilerplate in C#...

        public GameWindow(ClientGame theGame) {
            InitializeComponent();
            game = theGame;
            game.OnMapChange += (_, __) => PlaceObjects(GUIMap, game.Map);
        }

        private static void placeWalker(string texture, IWalkerExtended walker, Canvas c) {
            var e = new Ellipse() { Width = walker.Radius*2, Height = walker.Radius*2 };
            e.Fill = (ImageBrush)Application.Current.Resources[texture];
            //e.Stroke = Brushes.Gray;
            //e.StrokeThickness = 0.05;
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
            foreach (var o in m.Obstacles) placeObstacle(o, c);
            foreach (var s in m.ResupplyPoints) placeResupply(s, c);
            foreach (var h in m.Humans) placeWalker("human", h, c);
            foreach (var z in m.Zombies) placeWalker("zombie", z, c);
        }

        private void Window_Resized(object sender, SizeChangedEventArgs e) {
            GUIMap.RenderTransform = new ScaleTransform(GUIMap.ActualWidth / game.Width, GUIMap.ActualHeight / game.Height);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            var messages = new[] {
                "Are you sure you want to leave?  Real work is much worse.",
                "You're trying to say you like TV better than me, right?",
                "You know, next time you play, I'm going to get you.",
                "You want to leave?  Go ahead, see if I care.",
                "Get out of here and go back to your boring programs.",
                "Look, bud.  You leave now and you forfeit your body count.",
                "Just leave.  When you come back, I'll be waiting with a bat."
            };
            if (MessageBox.Show(messages.PickOne(), "Leave Game", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) {
                game.Dispose();
                Application.Current.Shutdown(0);
            } else {
                e.Cancel = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            PlaceObjects(GUIMap, game.Map);
        }
    }
}
