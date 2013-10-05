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

namespace HvZ.Client {
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    partial class ClientWindow : Window {
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

        private void Maps_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count == 0) return; // nothing to do.
            var filename = (string)e.AddedItems[0];
            try {
                var m = new Map(MAP_LOCATION + "\\" + filename + ".txt");
                GameWindow.PlaceObjects(MapPreview, m);
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
            var gameWindow = new GameWindow(Name_textBox.Text, (Role.SelectedItem as ComboBoxItem).Content.ToString(), m, new HvZ.AI.GreedyHuman());
            gameWindow.Owner = this;
            gameWindow.Show();
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}
