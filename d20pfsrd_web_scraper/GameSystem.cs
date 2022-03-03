namespace d20pfsrd_web_scraper;

public static class GameSystem
{
     public static string Pathfinder1E = "https://www.d20pfsrd.com";
     public static string DnD5E = "https://www.5esrd.com";

     public static Dictionary<string, string> GameSystemToLink = new Dictionary<string, string>()
     {
          {"pathfinder_1e", Pathfinder1E},
          {"dnd_5e", DnD5E},
     };
     
     public static Dictionary<string, string> GameSystemToPrefix = new Dictionary<string, string>()
     {
          {"pathfinder_1e", "d20pfsrd"},
          {"dnd_5e", "5esrd"},
     };
     
     public static Dictionary<string, string> GameSystemToTitlePostfix = new Dictionary<string, string>()
     {
          {"pathfinder_1e", " &#8211; d20PFSRD"},
          {"dnd_5e", " &#8211; 5th edition SRD"},
     };
}