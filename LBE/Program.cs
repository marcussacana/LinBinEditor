using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LBE
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] Args)
        {
            Console.Write("Maked by marcus-beta to Yuu-chan from Meow Works\nVisit: http://katawa.url.ph");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(Args));
        }
    }
}
