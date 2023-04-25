using System;
using System.Windows.Forms;

namespace ProjektNr1_Sikorska
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PsProjektNr1_Sikorska());
        }
    }
}
