using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Pipes;
using System.IO;

namespace lab_6
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        static void Main()
        {
            try
            {
                var client = new NamedPipeClientStream("KeyBot");
                client.Connect(500);
                StreamReader reader = new StreamReader(client);
                string[] ParsedParams = reader.ReadLine().Split('|');
                if (ParsedParams[0] == null || MessageBox.Show(string.Format("{0}, Do You want to open the program in \"{1}\" version?", ParsedParams[1], ParsedParams[0]), "Program Name", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
               }
                else
                    return;        
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
                ///return;
            }          
        }
    }
}
