using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace HvZClient {
    /// <summary>Interaction logic for App.xaml</summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            ClientWindow win = new ClientWindow();
            win.Show();
            Game.StartProcesses();
        }
    }
}
