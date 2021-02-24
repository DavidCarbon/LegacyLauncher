using ClassicGameLauncher.App.Classes.LauncherCore.Client.Auth;
using ClassicGameLauncher.App.Classes.LauncherCore.Client.Web;
using ClassicGameLauncher.App.Classes.LauncherCore.Global;
using ClassicGameLauncher.App.Classes.SystemPlatform.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicGameLauncher.App.Classes.LauncherCore
{
    class AntiCheat
    {
        public static int process_id = 0;
        public static string serverip = Form1.SelectedServerIP;
        public static string user_id = Tokens.UserId;
        public static string persona_name = String.Empty;
        public static string persona_id = String.Empty;
        public static int event_id = 2000164;
        public static int cheats_detected = 0;
        private static Thread thread = new Thread(() => { });

        //INTERNAL//
        public static bool detect_MULTIHACK = false;
        public static bool detect_FAST_POWERUPS = false;
        public static bool detect_SPEEDHACK = false;
        public static bool detect_SMOOTH_WALLS = false;
        public static bool detect_TANK_MODE = false;
        public static bool detect_WALLHACK = false;
        public static bool detect_DRIFTMOD = false;
        public static bool detect_PURSUITBOT = false;
        public static bool detect_PMASKER = false;

        public static void MemoryChecks()
        {
            Process process = Process.GetProcessById(process_id);
            IntPtr processHandle = Kernel32.OpenProcess(0x0010, false, process.Id);
            int baseAddress = process.MainModule.BaseAddress.ToInt32();

            thread = new Thread(() =>
            {
                List<int> addresses = new List<int> {
                    418534,  // GMZ_MULTIHACK
                    3788216, // FAST_POWERUPS
                    4552702, // SPEEDHACK
                    4476396, // SMOOTH_WALLS
                    4506534, // TANK
                    4587060, // WALLHACK
                    4486168, // DRIFTMOD/MULTIHACK
                    4820249, // PURSUITBOT (NO COPS VARIATION)
                    8972152 // PROFILEMASKER!
                };

                while (true)
                {
                    foreach (var oneAddress in addresses)
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[4];
                        Kernel32.ReadProcessMemory((int)processHandle, baseAddress + oneAddress, buffer, buffer.Length, ref bytesRead);

                        String checkInt = "0x" + BitConverter.ToString(buffer).Replace("-", String.Empty);

                        if (oneAddress == 418534 && checkInt != "0x3B010F84" && detect_MULTIHACK == false) { detect_MULTIHACK = true; }
                        if (oneAddress == 3788216 && checkInt != "0x807DFB00" && detect_FAST_POWERUPS == false) { detect_FAST_POWERUPS = true; }
                        if (oneAddress == 4552702 && checkInt != "0x76390F2E" && detect_SPEEDHACK == false) { detect_SPEEDHACK = true; }
                        if (oneAddress == 4476396 && checkInt != "0x84C00F84" && detect_SMOOTH_WALLS == false) { detect_SMOOTH_WALLS = true; }
                        if (oneAddress == 4506534 && checkInt != "0x74170F57" && detect_TANK_MODE == false) { detect_TANK_MODE = true; }
                        if (oneAddress == 4587060 && checkInt != "0x74228B16" && detect_WALLHACK == false) { detect_WALLHACK = true; }
                        if (oneAddress == 4820249 && checkInt != "0x0F845403" && detect_PURSUITBOT == false) { detect_PURSUITBOT = true; }
                        if (oneAddress == 4486168 && checkInt != "0xF30F1086")
                        {
                            if (checkInt.Substring(0, 4) == "0xE8" && detect_MULTIHACK == false) { detect_MULTIHACK = true; }
                            if (checkInt.Substring(0, 4) == "0xE9" && detect_DRIFTMOD == false) { detect_DRIFTMOD = true; }
                        }

                        //ProfileMasker
                        if (oneAddress == 8972152)
                        {
                            byte[] buffer16 = new byte[16];

                            Kernel32.ReadProcessMemory((int)processHandle, (int)(BitConverter.ToUInt32(buffer, 0) + 0x89), buffer16, buffer16.Length, ref bytesRead);
                            String MemoryUsername = Encoding.UTF8.GetString(buffer16, 0, buffer16.Length);
                        }
                    }

                    Task.Delay(1000);

                    if (detect_MULTIHACK == true) AntiCheat.cheats_detected |= 1;
                    if (detect_FAST_POWERUPS == true) AntiCheat.cheats_detected |= 2;
                    if (detect_SPEEDHACK == true) AntiCheat.cheats_detected |= 4;
                    if (detect_SMOOTH_WALLS == true) AntiCheat.cheats_detected |= 8;
                    if (detect_TANK_MODE == true) AntiCheat.cheats_detected |= 16;
                    if (detect_WALLHACK == true) AntiCheat.cheats_detected |= 32;
                    if (detect_DRIFTMOD == true) AntiCheat.cheats_detected |= 64;
                    if (detect_PURSUITBOT == true) AntiCheat.cheats_detected |= 128;
                    if (detect_PMASKER == true) AntiCheat.cheats_detected |= 256;

                    if (AntiCheat.cheats_detected >= 0)
                    {
                        Form1.CheatsWasUsed = true;
                        Form1._gameKilledBySpeedBugCheck = true;
                        thread.Abort();
                    }
                }
            })
            { IsBackground = true };
            thread.Start();
        }
    }
}
