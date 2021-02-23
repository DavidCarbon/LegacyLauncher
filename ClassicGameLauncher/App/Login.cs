﻿using ClassicGameLauncher.App.Classes;
using ClassicGameLauncher.App.Classes.Global;
using ClassicGameLauncher.App.Classes.Hashes;
using ClassicGameLauncher.App.Classes.ModNet;
using GameLauncher.App.Classes;
using GameLauncher.App.Classes.Auth;
using GameLauncherReborn;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ClassicGameLauncher
{
    public partial class Form1 : Form {
        /* START Login Checks */
        private bool _modernAuthSupport = false;
        private bool _ticketRequired;
        /* END Login Checks */

        /* START ModNet Global Functions */
        public static String ModNetFileNameInUse = String.Empty;
        readonly Queue<Uri> modFilesDownloadUrls = new Queue<Uri>();
        bool isDownloadingModNetFiles = false;
        int CurrentModFileCount = 0;
        int TotalModFileCount = 0;
        /* END ModNet Global Functions */

        /* START SpeedBug Timer */
        private bool _gameKilledBySpeedBugCheck = false;
        private int _nfswPid;
        /* END SpeedBug Timer */

        /* START GetServerInformation Cache */
        SimpleJSON.JSONNode result;
        /* END GetServerInformation Cache  */

        public Form1() {
            InitializeComponent();

            Load += new EventHandler(Form1_Load);
            serverText.SelectedIndexChanged += new EventHandler(serverPick_SelectedIndexChanged);

            actionText.Text = "Ready!";
        }

        private void Form1_Load(object sender, EventArgs e) {
            var response = "";
            try {
                WebClient wc = new WebClient();
                string serverurl = "http://api2-sbrw.davidcarbon.download/serverlist.txt";
                response = wc.DownloadString(serverurl);
            } catch (Exception) { }

            serverText.DisplayMember = "Text";
            serverText.ValueMember = "Value";

            List<Object> items = new List<Object>();

            String[] substrings = response.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var substring in substrings) {
                if (!String.IsNullOrEmpty(substring)) {
                    String[] substrings22 = substring.Split(new string[] { ";" }, StringSplitOptions.None);
                    items.Add(new { Text = substrings22[0], Value = substrings22[1] });
                }
            }

            serverText.DataSource = items;
            serverText.SelectedIndex = 0;
        }

        private void serverPick_SelectedIndexChanged(object sender, EventArgs e) {
            Tokens.Clear();
            actionText.Text = "Loading info...";

            try {
                button1.Enabled = true;
                button2.Enabled = true;

                WebClientWithTimeout serverval = new WebClientWithTimeout();
                var stringToUri = new Uri(serverText.SelectedValue.ToString() + "/GetServerInformation");
                String serverdata = serverval.DownloadString(stringToUri);

                result = SimpleJSON.JSON.Parse(serverdata);

                actionText.Text = "Players on server: " + result["onlineNumber"];

                try {
                    if (string.IsNullOrEmpty(result["modernAuthSupport"])) {
                        _modernAuthSupport = false;
                    } else if (result["modernAuthSupport"]) {
                        if (stringToUri.Scheme == "https") {
                            _modernAuthSupport = true;
                        } else {
                            _modernAuthSupport = false;
                        }
                    } else {
                        _modernAuthSupport = false;
                    }
                } catch {
                    _modernAuthSupport = false;
                }

                try {
                    _ticketRequired = (bool)result["requireTicket"];
                } catch {
                    _ticketRequired = true; //lets assume yes, we gonna check later if ticket is empty or not.
                }
            } catch {
                button1.Enabled = false;
                button2.Enabled = false;
                actionText.Text = "Server is offline.";
            }


            ticketBox.Enabled = _ticketRequired;
        }

        private void button1_Click(object sender, EventArgs e) {
            Tokens.Clear();
            if (!validateEmail(loginEmailBox.Text)) {
                actionText.Text = "Please type your email!";
            } else if (String.IsNullOrEmpty(loginPasswordBox.Text)) {
                actionText.Text = "Please type your password!";
            } else {
                Tokens.IPAddress = serverText.SelectedValue.ToString();
                Tokens.ServerName = serverText.SelectedItem.ToString();

                if (_modernAuthSupport == false) {
                    ClassicAuth.Login(loginEmailBox.Text, SHA.HashPassword(loginPasswordBox.Text).ToLower());
                } else {
                    ModernAuth.Login(loginEmailBox.Text, loginPasswordBox.Text);
                }

                if (String.IsNullOrEmpty(Tokens.Error)) {
                    if (!String.IsNullOrEmpty(Tokens.Warning)) {
                        MessageBox.Show(null, Tokens.Warning, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    //TODO: MODS GOES HERE
                    DoModNetJob();
                    //
                } else {
                    MessageBox.Show(null, Tokens.Error, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    actionText.Text = (String.IsNullOrEmpty(Tokens.Error)) ? "An error occurred." : Tokens.Error;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e) {
            if (!validateEmail(registerEmail.Text)) {
                actionText.Text = "Please type your email!";
            } else if (String.IsNullOrEmpty(registerPassword.Text)) {
                actionText.Text = "Please type your password!";
            } else if (String.IsNullOrEmpty(registerPassword2.Text)) {
                actionText.Text = "Please type your confirmation password!";
            } else if (registerPassword.Text != registerPassword2.Text) {
                actionText.Text = "Password doesn't match!";
            } else if(_ticketRequired) {
                if(String.IsNullOrEmpty(ticketBox.Text)) {
                    actionText.Text = "Ticket is required to play on this server!";
                } else {
                    createAccount();
                }
            } else {
                createAccount();
            }
        }

        private void createAccount() {
            String token = (_ticketRequired) ? ticketBox.Text : null;
            Tokens.IPAddress = serverText.SelectedValue.ToString();
            Tokens.ServerName = serverText.SelectedItem.ToString();

            if (_modernAuthSupport == false) {
                ClassicAuth.Register(registerEmail.Text, SHA.HashPassword(registerPassword.Text), token);
            } else {
                ModernAuth.Register(registerEmail.Text, registerPassword.Text, token);
            }

            if (!String.IsNullOrEmpty(Tokens.Success)) {
                MessageBox.Show(null, Tokens.Success, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                actionText.Text = Tokens.Success;

                tabControl1.Visible = true;
            } else {
                MessageBox.Show(null, Tokens.Error, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                actionText.Text = Tokens.Error;
            }
        }

        public static bool validateEmail(string email) {
            if (String.IsNullOrEmpty(email)) return false;

            String theEmailPattern = @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
                                   + "@"
                                   + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$";

            return Regex.IsMatch(email, theEmailPattern);
        }

        public void launchGame() {
           
            ProcessStartInfo psi = new ProcessStartInfo();

            psi.WorkingDirectory = Directory.GetCurrentDirectory();
            psi.FileName = "nfsw.exe";
            psi.Arguments = "EU " + Tokens.IPAddress + " " + Tokens.LoginToken + " " + Tokens.UserId;

            Process.Start(psi);
        }

        private void LaunchGame(Form x)
        {
            actionText.Text = "Launching game...";
            Application.DoEvents();

            var args = "EU " + Tokens.IPAddress + " " + Tokens.LoginToken + " " + Tokens.UserId;
            var psi = new ProcessStartInfo();


            psi.WorkingDirectory = Directory.GetCurrentDirectory();
            psi.FileName = "nfsw.exe";
            psi.Arguments = args;

            var nfswProcess = Process.Start(psi);
            nfswProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

            var processorAffinity = 0;
            for (var i = 0; i < Math.Min(Math.Max(1, Environment.ProcessorCount), 8); i++)
            {
                processorAffinity |= 1 << i;
            }

            nfswProcess.ProcessorAffinity = (IntPtr)processorAffinity;

            AntiCheat.process_id = nfswProcess.Id;

            //TIMER HERE
            int secondsToShutDown = (result["secondsToShutDown"].AsInt != 0) ? result["secondsToShutDown"].AsInt : 2 * 60 * 60;
            System.Timers.Timer shutdowntimer = new System.Timers.Timer();
            shutdowntimer.Elapsed += (x2, y2) =>
            {
                Process[] allOfThem = Process.GetProcessesByName("nfsw");

                if (secondsToShutDown <= 0)
                {
                    foreach (var oneProcess in allOfThem)
                    {
                        Process.GetProcessById(oneProcess.Id).Kill();
                    }
                }

                //change title

                foreach (var oneProcess in allOfThem)
                {
                    long p = oneProcess.MainWindowHandle.ToInt64();
                    TimeSpan t = TimeSpan.FromSeconds(secondsToShutDown);
                    string secondsToShutDownNamed = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);

                    if (secondsToShutDown == 0)
                    {
                        secondsToShutDownNamed = "Waiting for event to finish.";
                    }

                    User32.SetWindowText((IntPtr)p, "NEED FOR SPEED™ WORLD | Server: " + serverText.SelectedValue.ToString() + " | Launcher Build: " + ProductVersion + " | Force Restart In: " + secondsToShutDownNamed);
                }

                --secondsToShutDown;
            };

            shutdowntimer.Interval = 1000;
            shutdowntimer.Enabled = true;

            if (nfswProcess != null)
            {
                nfswProcess.EnableRaisingEvents = true;
                _nfswPid = nfswProcess.Id;

                nfswProcess.Exited += (sender2, e2) =>
                {
                    _nfswPid = 0;
                    var exitCode = nfswProcess.ExitCode;

                    if (_gameKilledBySpeedBugCheck == true) exitCode = 2137;

                    if (exitCode == 0)
                    {
                        Application.Exit();
                    }
                    else
                    {
                        x.BeginInvoke(new Action(() =>
                        {
                            x.WindowState = FormWindowState.Normal;
                            x.Opacity = 1;
                            x.ShowInTaskbar = true;
                            String errorMsg = "Game Crash with exitcode: " + exitCode.ToString() + " (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073741819) errorMsg = "Game Crash: Access Violation (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073740940) errorMsg = "Game Crash: Heap Corruption (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073740791) errorMsg = "Game Crash: Stack buffer overflow (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -805306369) errorMsg = "Game Crash: Application Hang (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073741515) errorMsg = "Game Crash: Missing dependency files (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073740972) errorMsg = "Game Crash: Debugger crash (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == -1073741676) errorMsg = "Game Crash: Division by Zero (0x" + exitCode.ToString("X") + ")";
                            if (exitCode == 1) errorMsg = "The process nfsw.exe was killed via Task Manager";
                            if (exitCode == 2137) errorMsg = "Launcher killed your game to prevent SpeedBugging.";
                            if (exitCode == -3) errorMsg = "The Server was unable to resolve the request";
                            if (exitCode == -4) errorMsg = "Another instance is already executed";
                            if (exitCode == -5) errorMsg = "DirectX Device was not found. Please install GPU Drivers before playing";
                            if (exitCode == -6) errorMsg = "Server was unable to resolve your request";
                            //ModLoader
                            if (exitCode == 2) errorMsg = "ModNet: Game was launched with invalid command line parameters.";
                            if (exitCode == 3) errorMsg = "ModNet: .links file should not exist upon startup!";
                            if (exitCode == 4) errorMsg = "ModNet: An Unhandled Error Appeared";
                            actionText.Text = errorMsg.ToUpper();
                            if (_nfswPid != 0)
                            {
                                try
                                {
                                    Process.GetProcessById(_nfswPid).Kill();
                                }
                                catch { /* ignored */ }
                            }

                            DialogResult restartApp = MessageBox.Show(null, errorMsg + "\nWould you like to restart the launcher?", "GameLauncher", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (restartApp == DialogResult.Yes)
                            {
                                Application.Restart();
                                Application.ExitThread();
                            }
                            Application.Exit();
                        }));
                    }
                };
            }
        }

        private string FormatFileSize(long byteCount) {
            var numArray = new double[] { 1000000000, 1000000, 1000, 0 };
            var strArrays = new[] { "GB", "MB", "KB", "Bytes" };
            for (var i = 0; i < numArray.Length; i++) {
                if (byteCount >= numArray[i]) {
                    return string.Concat($"{byteCount / numArray[i]:0.00} ", strArrays[i]);
                }
            }

            return "0 Bytes";
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            this.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                actionText.Text = ("Downloaded " + FormatFileSize(e.BytesReceived) + " of " + FormatFileSize(e.TotalBytesToReceive));
            });
        }

        public void DoModNetJob() {
            if (!Directory.Exists("modules")) Directory.Delete("modules", true);
            if (!Directory.Exists("scripts")) Directory.CreateDirectory("scripts");

            String[] RemoveAllFiles = new string[] { "modules/udpcrc.soapbox.module", "modules/udpcrypt1.soapbox.module", "modules/udpcrypt2.soapbox.module", 
                "modules/xmppsubject.soapbox.module", "scripts/global.ini", "lightfx.dll", "PocoFoundation.dll", "PocoNet.dll", "ModManager.dat"};

            foreach (string file in RemoveAllFiles) 
            {
                if (File.Exists(file)) 
                {
                    try {
                        File.Delete(file);
                    } catch {
                        MessageBox.Show($"File {file} cannot be deleted.", "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            
            String jsonModNet = ModNetReloaded.ModNetSupported(serverText.SelectedValue.ToString());

            if (jsonModNet != String.Empty) {
                actionText.Text = "Detecting ModNetSupport for " + serverText.SelectedItem.ToString();

                try {
                    try { if (File.Exists("lightfx.dll")) File.Delete("lightfx.dll"); } catch { }

                    /* Get Remote ModNet list to process for checking required ModNet files are present and current */
                    String modules = new WebClient().DownloadString(URLs.modnetserver + "/launcher-modules/modules.json");
                    string[] modules_newlines = modules.Split(new string[] { "\n" }, StringSplitOptions.None);

                    foreach (String modules_newline in modules_newlines)
                    {
                        if (modules_newline.Trim() == "{" || modules_newline.Trim() == "}") continue;

                        String trim_modules_newline = modules_newline.Trim();
                        String[] modules_files = trim_modules_newline.Split(new char[] { ':' });

                        String ModNetList = modules_files[0].Replace("\"", "").Trim();
                        String ModNetSHA = modules_files[1].Replace("\"", "").Replace(",", "").Trim();

                        if (SHATwoFiveSix.HashFile(AppDomain.CurrentDomain.BaseDirectory + "\\" + ModNetList).ToLower() != ModNetSHA || !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + ModNetList))
                        {
                            actionText.Text = ("ModNet: Downloading " + ModNetList).ToUpper();

                            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + ModNetList))
                            {
                                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\" + ModNetList);
                            }

                            WebClient newModNetFilesDownload = new WebClient();
                            newModNetFilesDownload.DownloadFile(URLs.modnetserver + "/launcher-modules/" + ModNetList, AppDomain.CurrentDomain.BaseDirectory + "/" + ModNetList);
                        }
                        else
                        {
                            actionText.Text = ("ModNet: Up to Date " + ModNetList).ToUpper();
                        }

                        Application.DoEvents();
                    }

                    SimpleJSON.JSONNode MainJson = SimpleJSON.JSON.Parse(jsonModNet);

                    Uri newIndexFile = new Uri(MainJson["basePath"] + "/index.json");
                    String jsonindex = new WebClient().DownloadString(newIndexFile);

                    SimpleJSON.JSONNode IndexJson = SimpleJSON.JSON.Parse(jsonindex);


                    String path = Path.Combine("MODS", MDFive.HashPassword(MainJson["serverID"]).ToLower());
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    /* new */
                    foreach (JSONNode modfile in IndexJson["entries"])
                    {
                        if (SHA.HashFile(path + "/" + modfile["Name"]).ToLower() != modfile["Checksum"])
                        {
                            modFilesDownloadUrls.Enqueue(new Uri(MainJson["basePath"] + "/" + modfile["Name"]));
                            TotalModFileCount++;

                            if (File.Exists(path + "/" + modfile["Name"]))
                            {
                                File.Delete(path + "/" + modfile["Name"]);
                            }
                        }
                    }

                    if (modFilesDownloadUrls.Count != 0)
                    {
                        this.DownloadModNetFilesRightNow(path);
                    }
                    else
                    {
                        LaunchGame(null);
                        //launchGame();
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            } 
            else 
            {
                //Yikes from me Coders - DavidCarbon
                LaunchGame(null);
                //launchGame();
            }
        }

        public void DownloadModNetFilesRightNow(string path)
        {
            while (isDownloadingModNetFiles == false)
            {
                CurrentModFileCount++;
                var url = modFilesDownloadUrls.Dequeue();
                string FileName = url.ToString().Substring(url.ToString().LastIndexOf("/") + 1, (url.ToString().Length - url.ToString().LastIndexOf("/") - 1));

                ModNetFileNameInUse = FileName;

                try
                {
                    WebClient client2 = new WebClient();

                    client2.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client2.DownloadFileCompleted += (test, stuff) =>
                    {
                        isDownloadingModNetFiles = false;
                        if (modFilesDownloadUrls.Any() == false)
                        {
                            LaunchGame(null);
                            //launchGame();
                        }
                        else
                        {
                            //Redownload other file
                            DownloadModNetFilesRightNow(path);
                        }
                    };
                    client2.DownloadFileAsync(url, path + "/" + FileName);
                }
                catch (Exception error)
                {
                    actionText.Text = error.Message;
                }

                isDownloadingModNetFiles = true;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) 
        {
            if (!string.IsNullOrEmpty(result["WebRecoveryUrl"]))
            {
                Process.Start(result["WebRecoveryUrl"]);
                MessageBox.Show(null, "A browser window has been opened to complete password recovery on " + result["ServerName"], "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                string send = Prompt.ShowDialog("Please specify your email address.", "LegacyLauncher");

                if (send != String.Empty)
                {
                    String responseString;
                    try
                    {
                        Uri resetPasswordUrl = new Uri(serverText.SelectedValue.ToString() + "/RecoveryPassword/forgotPassword");

                        var request = (HttpWebRequest)System.Net.WebRequest.Create(resetPasswordUrl);
                        var postData = "email=" + send;
                        var data = Encoding.ASCII.GetBytes(postData);
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.ContentLength = data.Length;

                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(data, 0, data.Length);
                        }

                        var response = (HttpWebResponse)request.GetResponse();
                        responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    }
                    catch
                    {
                        responseString = "Failed to send email!";
                    }

                    MessageBox.Show(null, responseString, "GameLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
