using System;
using System.Windows.Forms;

namespace ActionMarque
{
    class TestMain
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Console.WriteLine("Test du graphique avec extension jusqu'en 2025...");
            TestSimple.TestChartDisplay();
        }
    }
}
