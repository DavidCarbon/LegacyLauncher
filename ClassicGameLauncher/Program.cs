using ClassicGameLauncher.App.Classes.Hashes;
using ClassicGameLauncher.App.Classes.ModNet;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ClassicGameLauncher
{
    static class Program {
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!File.Exists("nfsw.exe"))
            {
                MessageBox.Show("nfsw.exe not found! Please put this launcher in the game directory. " +
                    "If you don't have the game installed yet use the new launcher to install it (visit https://soapboxrace.world/)",
                    "LegacyLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!canAccesGameData())
            {
                MessageBox.Show("This application requires admin priviledge. Restarting...");
                runAsAdmin();
                return;
            }

            if (SHA.HashFile("nfsw.exe") != "7C0D6EE08EB1EDA67D5E5087DDA3762182CDE4AC")
            { 
                MessageBox.Show("Invalid file was detected, please restore original nfsw.exe", "LegacyLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } 
            else 
            {
                if (File.Exists(".links"))
                {
                    var linksPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\.links");
                    ModNetLinksCleanup.CleanLinks(linksPath);
                }

                Application.Run(new Form1());
            }
        }

        static bool canAccesGameData()
        {
            try
            {
                using (var test = File.OpenRead("nfsw.exe"))
                {
                    
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return true;
        }

        public static void runAsAdmin()
        {
            string[] args = Environment.GetCommandLineArgs();

            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                Verb = "runas",
                FileName = Application.ExecutablePath
            };

            if ((int)args.Length > 0)
            {
                processStartInfo.Arguments = args[0];
            }

            try
            {
                Process.Start(processStartInfo);
            }
            catch (Exception exception1)
            {
                MessageBox.Show("Failed to self-run as admin: " + exception1.Message);
            }
        }
    }
}
