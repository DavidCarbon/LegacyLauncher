using System.Windows.Forms;

namespace ClassicGameLauncher.App.Classes.LauncherCore.Client.Web
{
    class UserAgent
    {
        public static string ProjectLink = "(+https://github.com/SoapBoxRaceWorld/LegacyLauncher)";
        public static string ProjectAltLink = "WinForms (+https://github.com/SoapBoxRaceWorld/LegacyLauncher)";
        public static string AgentName = "GameLauncher-Legacy";
        public static string AgentAltName = "GameLauncherLegacy";
        public static string Build = Application.ProductVersion;


        public static string UserAgentName = AgentName + " " + ProjectLink;
        public static string UserAgentNameWithBuild = AgentAltName + " " + Build;
        public static string UserAgentHeaderName = AgentAltName + " " + Build + " " + ProjectAltLink;
    }
}
