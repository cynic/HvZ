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
        // WPF uses retained-mode graphics, not immediate-mode.  See http://msdn.microsoft.com/en-us/library/ms748373.aspx#visual_rendering_behavior .  No need to simulate immediate-mode.
        // A better plan might be to do most of this stuff in XAML, using DataTemplates http://msdn.microsoft.com/en-us/library/ms742521.aspx
        // Let's see if I can get away with implementing INotifyPropertyChanged.  Necessary, but such boilerplate in C#...

        private GameWindow(string name, string role, Map m) {
            InitializeComponent();
            Title = name + " - " + role;
            ground.Background = ClientWindow.ImageFromMap(m);
        }

        public GameWindow(string name, string role, Map m, IZombieAI ai) : this(name, role, m) {
            game = new ClientGame(Dispatcher, name, role, m, ai);
            game.OnPlayerJoin += (_, __) => placeObjects();
        }

        public GameWindow(string name, string role, Map m, IHumanAI ai) : this(name, role, m) {
            game = new ClientGame(Dispatcher, name, role, m, ai);
            game.OnPlayerJoin += (_, __) => placeObjects();
        }

        private void placeWalker(string texture, IWalker walker) {
            var e = new Ellipse() { Width = walker.Radius*2, Height = walker.Radius*2 };
            e.Fill = (ImageBrush)Resources[texture];
            var group = new TransformGroup();
            var translate = new TranslateTransform(walker.Position.X - walker.Radius, walker.Position.Y - walker.Radius);
            var rotate = new RotateTransform(walker.Heading, walker.Radius, walker.Radius);
            group.Children.Add(rotate);
            group.Children.Add(translate);
            e.RenderTransform = group;
            // HOLY FRACKING SPACE-POPE ON A POGO-STICK.
            // The commented-out bits below are what's required for WPF to do a simple animation.
            // ... really, WPF?
            // ... really, really?
            // Screw this hippie BS.
            /*
            var x = walker.Position.X - walker.Radius;
            var y = walker.Position.Y - walker.Radius;
            var storyboard = new Storyboard();
            */
            walker.Position.PropertyChanged += (_, __) => {
                /*
                var animX = new DoubleAnimation(x, walker.Position.X - walker.Radius, new Duration(TimeSpan.FromMilliseconds(100)));
                x = walker.Position.X - walker.Radius;
                var animY = new DoubleAnimation(y, walker.Position.Y - walker.Radius, new Duration(TimeSpan.FromMilliseconds(100)));
                y = walker.Position.Y - walker.Radius;
                storyboard.Children.Add(animX);
                storyboard.Children.Add(animY);
                var propertyChainX = new[] {
                    Ellipse.RenderTransformProperty,
                    TransformGroup.ChildrenProperty,
                    TranslateTransform.XProperty
                };
                var propertyChainY = new[] {
                    Ellipse.RenderTransformProperty,
                    TransformGroup.ChildrenProperty,
                    TranslateTransform.YProperty
                };
                string path = "(0).(1)[1].(2)";
                var ppathX = new PropertyPath(path, propertyChainX);
                var ppathY = new PropertyPath(path, propertyChainY);
                Storyboard.SetTarget(animX, e);
                Storyboard.SetTarget(animY, e);
                Storyboard.SetTargetProperty(animX, ppathX);
                Storyboard.SetTargetProperty(animY, ppathY);
                storyboard.Begin();
                */
                translate.X = walker.Position.X - walker.Radius;
                translate.Y = walker.Position.Y - walker.Radius;
            };
            walker.PropertyChanged += (_, __) => {
                rotate.Angle = walker.Heading;
            };
            GUIMap.Children.Add(e);
        }

        private void placeObstacle(Obstacle item) {
            var e = new Ellipse() { Width = item.Radius * 2, Height = item.Radius * 2 };
            e.Fill = (ImageBrush)Resources["obstacle"];
            e.Opacity = 0.65;
            var translate = new TranslateTransform(item.Position.X - item.Radius, item.Position.Y - item.Radius);
            e.RenderTransform = translate;
            GUIMap.Children.Add(e);
        }

        private void placeResupply(ResupplyPoint item) {
            var e = new Ellipse() { Width = item.Radius * 2, Height = item.Radius * 2 };
            e.Fill = (ImageBrush)Resources["supply"];
            e.Stroke = Brushes.Black;
            e.StrokeThickness = 0.1;
            var translate = new TranslateTransform(item.Position.X - item.Radius, item.Position.Y - item.Radius);
            e.RenderTransform = translate;
            GUIMap.Children.Add(e);
        }

        private void placeObjects() {
            GUIMap.Children.Clear();
            GUIMap.RenderTransform = new ScaleTransform(GUIMap.ActualWidth / game.Width, GUIMap.ActualHeight / game.Height);
            foreach (var o in game.Map.Obstacles) placeObstacle(o);
            foreach (var s in game.Map.ResupplyPoints) placeResupply(s);
            foreach (var h in game.Map.Humans) placeWalker("human", h);
            foreach (var z in game.Map.Zombies) placeWalker("zombie", z);
        }

        private void Window_Resized(object sender, SizeChangedEventArgs e) {
            GUIMap.RenderTransform = new ScaleTransform(GUIMap.ActualWidth / game.Width, GUIMap.ActualHeight / game.Height);
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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            placeObjects();
        }
    }
}
