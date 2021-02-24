namespace ClassicGameLauncher.App.Classes.LauncherCore.Global
{
    class URLs
    {
        public static string mainserver = "http://api.worldunited.gg";

        public static string staticapiserver = "http://api-sbrw.davidcarbon.download";

        public static string secondstaticapiserver = "http://api2-sbrw.davidcarbon.download";

        public static string modnetserver = "http://cdn.soapboxrace.world";


        public static string[] serverlisturl = new string[]
        {
            mainserver + "/serverlist.txt",
            staticapiserver + "/serverlist.txt"
        };

        public static string[] anticheatreporting = new string[]
        {
            mainserver + "/report",
            "http://anticheat.worldonline.pl/report",
            "http://la-sbrw.davidcarbon.download/report?",
            "http://la2-sbrw.davidcarbon.download/report?"
        };
    }
}
