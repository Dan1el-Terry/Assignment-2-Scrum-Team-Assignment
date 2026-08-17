using System;
using System.Windows;

namespace Assign_2
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow window = new MainWindow();

            app.Run(window);
        }
    }
}