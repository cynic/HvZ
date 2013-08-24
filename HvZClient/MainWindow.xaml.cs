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
using HvZCommon;

namespace HvZClient {
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window {
        private static readonly List<Key> pressedKeys = new List<Key>();

        public MainWindow() {
            InitializeComponent();

            Zombie z = new Zombie();
            GUIMap.Children.Add(new Image() {
                Source = Resource.getResourceByName("zombie").Image,
                Width = 45,
                Height = 45,
            });
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
    }
}
