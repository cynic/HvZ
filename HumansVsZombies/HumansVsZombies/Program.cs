using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HvZ;
using System.Text.RegularExpressions;

namespace HumansVsZombies
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Map m = new Map(@"C:\Users\Yusuf Motara\Desktop\maps\verticalwalls.txt");
            Game game = new Game();
            game.CreateNewGame("Ardesia", m); // choose your own name, instead of "Ardesia"
            game.Join("Ardesia", new Taltos());
            //game.Join("Ardesia", new MyHumanAI());
            game.Join("Ardesia", new Teldra());
            game.Join("Ardesia", new Teldra());
            game.Display();
        }
    }
}
