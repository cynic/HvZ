using System;
using System.Collections.Generic;
using System.IO;
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
using HvZ.Common;

namespace HvZClient {
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window {
        HvZConnection connection = new HvZConnection();

        public ClientWindow() {
            InitializeComponent();
            // populate maps...
            foreach (var v in Directory.GetFiles("Maps", "*.txt")) {
                Maps.Items.Add(v);
            }
        }

        private ImageBrush ImageFromMap(string filename) {
            var m = new HvZ.Common.Map(filename);
            var arr = new byte[3 * m.Width * m.Height];
            var imgW = m.Width;
            var imgH = m.Height;
            for (int row = 0; row < m.Height; ++row) {
                for (int column = 0; column < m.Width; column++) {
                    Color rgb;
                    switch (m[column, row]) {
                        case HvZ.Common.Terrain.Empty: rgb = Colors.Black; break;
                        case HvZ.Common.Terrain.Ground: rgb = Colors.Brown; break;
                        case HvZ.Common.Terrain.Obstacle: rgb = Colors.Pink; break;
                        default: throw new InvalidOperationException("unrecognized mapitem");
                    }
                    int idx = 3 * (row * m.Width + column);
                    arr[idx] = rgb.R;
                    arr[idx + 1] = rgb.G;
                    arr[idx + 2] = rgb.B;
                }
            }
            var i = BitmapImage.Create(imgW, imgH, 96, 96, PixelFormats.Rgb24, null, arr, 3 * m.Width);
            /*
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(i));
            using (var fs = System.IO.File.Create("c:\\temp\\map1.png")) {
                encoder.Save(fs);
            }
            */
            var brush = new ImageBrush(i);
            brush.Stretch = Stretch.Fill;
            return brush;
        }

        private void Maps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 0) return; // nothing to do.
            var filename = (string)e.AddedItems[0];
            try {
                var img = ImageFromMap(filename);
                MapPreview.Background = img;
            } catch (Exception exc) {
                Maps.UnselectAll();
                MessageBox.Show(String.Format("I couldn't load this map.  Reason: {0}", exc.Message));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            var t = new System.Threading.Tasks.Task(new Action(() => {
                bool connected = false;
                while (!connected) {
                    try {
                        connection.ConnectToServer("localhost");
                        connected = true;
                        Dispatcher.Invoke(new Action(() => {
                            JoinButton.IsEnabled = CreateButton.IsEnabled = true;
                        }));
                    } catch {
                        System.Threading.Thread.Sleep(2000);
                    }
                }
            }));
            t.Start();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e) {
            if (String.IsNullOrWhiteSpace(Name.Text)) {
                MessageBox.Show("You need to type in a name first.");
                return;
            }
            if (Role.SelectedIndex == -1) {
                MessageBox.Show("You need to choose whether you're playing as a human or a zombie.");
                return;
            }
            if (Maps.SelectedIndex == -1) {
                MessageBox.Show("You need to choose a map that you want to play on.");
                return;
            }
            var m = new Map((string)Maps.SelectedValue);
            var gameWindow = new GameWindow(Name.Text, (Role.SelectedItem as ComboBoxItem).Content.ToString(), m, (HvZ.AI.IHumanAI)new HvZ.AI.RandomWalker());
            gameWindow.Owner = this;
            gameWindow.Show();
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}
