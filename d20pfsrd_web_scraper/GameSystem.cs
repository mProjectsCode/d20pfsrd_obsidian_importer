namespace d20pfsrd_web_scraper;

public static class GameSystem
{
     public const string Pathfinder1ELink = "https://www.d20pfsrd.com";
     public const string DnD5ELink = "https://www.5esrd.com";

     public const string PATHFINDER_1E = "pathfinder_1e";
     public const string DND_5E = "dnd_5e";

     public static Dictionary<string, string> GameSystemToLink = new Dictionary<string, string>()
     {
          {PATHFINDER_1E, Pathfinder1ELink},
          {DND_5E, DnD5ELink},
     };
     
     public static Dictionary<string, string> GameSystemToPrefix = new Dictionary<string, string>()
     {
          {PATHFINDER_1E, "d20pfsrd"},
          {DND_5E, "5esrd"},
     };
     
     public static Dictionary<string, string> GameSystemToTitlePostfix = new Dictionary<string, string>()
     {
          {PATHFINDER_1E, " &#8211; d20PFSRD"},
          {DND_5E, " &#8211; 5th edition SRD"},
     };
}