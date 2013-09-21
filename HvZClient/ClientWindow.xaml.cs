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
        internal static readonly string MAP_LOCATION = "Maps";

        private HvZConnection connection = new HvZConnection();

        public ClientWindow() {
            InitializeComponent();
            // populate maps...
            if (!Directory.Exists(MAP_LOCATION)) {
                Directory.CreateDirectory(MAP_LOCATION);
            }

            foreach (string v in Directory.GetFiles(MAP_LOCATION, "*.txt")) {
                Maps.Items.Add(System.IO.Path.GetFileNameWithoutExtension(v));
            }
        }

        protected override void OnContentRendered(EventArgs e) {
            QuoteGenerator.RegisterQuoteListener(quote);
            base.OnContentRendered(e);
            this.RegisterWindow();
        }

        public static ImageBrush ImageFromMap(string filename) {
            return ImageFromMap(new Map(filename));
        }

        public static ImageBrush ImageFromMap(Map m) {
            var arr = new byte[3 * m.Width * m.Height];
            var imgW = m.Width;
            var imgH = m.Height;
            for (int row = 0; row < m.Height; ++row) {
                for (int column = 0; column < m.Width; column++) {
                    Color rgb;
                    switch (m[column, row]) {
                        case HvZ.Common.Terrain.Empty: rgb = Colors.Black; break;
                        case HvZ.Common.Terrain.Ground: rgb = Colors.White; break;
                        default: throw new InvalidOperationException("unrecognized mapitem");
                    }
                    int idx = 3 * (row * m.Width + column);
                    arr[idx] = rgb.R;
                    arr[idx + 1] = rgb.G;
                    arr[idx + 2] = rgb.B;
                }
            }

            int mult = 6;
            arr = amplifyResolution(arr, mult, m.Width);
            var i = BitmapImage.Create(mult * imgW, mult * imgH, 96, 96, PixelFormats.Rgb24, null, arr, mult * 3 * m.Width);
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

        public static byte[] amplifyResolution(byte[] data, int mult, int width) {
            if (mult <= 1) {
                return data;
            }

            List<byte[][]> lines = new List<byte[][]>();
            List<byte[]> line = new List<byte[]>();

            for (int i = 0; i < data.Length; i += 3) {
                byte[] pixel = new byte[3];
                pixel[0] = data[i];
                pixel[1] = data[i + 1];
                pixel[2] = data[i + 2];

                for (int p = 0; p < mult; p++) {
                    line.Add(pixel);
                }

                if (line.Count == width * mult) {
                    for (int p = 0; p < mult; p++) {
                        lines.Add(line.ToArray());
                    }
                    line.Clear();
                }
            }

            List<byte> result = new List<byte>();
            foreach (byte[][] i in lines) {
                foreach (byte[] k in i) {
                    result.AddRange(k);
                }
            }

            return result.ToArray();
        }

        private void Maps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 0) return; // nothing to do.
            var filename = (string)e.AddedItems[0];
            try {
                var img = ImageFromMap(MAP_LOCATION + "\\" + filename + ".txt");
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
            if (String.IsNullOrWhiteSpace(Name_textBox.Text)) {
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
            var m = new Map(MAP_LOCATION + "\\" + (string)Maps.SelectedValue + ".txt");
            var gameWindow = new GameWindow(Name_textBox.Text, (Role.SelectedItem as ComboBoxItem).Content.ToString(), m, (HvZ.AI.IHumanAI)new HvZ.AI.RandomWalker());
            gameWindow.Owner = this;
            gameWindow.Show();
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}
