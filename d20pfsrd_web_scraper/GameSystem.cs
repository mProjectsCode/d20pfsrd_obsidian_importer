namespace d20pfsrd_web_scraper;

public static class GameSystem
{
    public const string PATHFINDER_1E = "pathfinder_1e";
    public const string DND_5E = "dnd_5e";

    public static Dictionary<string, string> GameSystemToLink = new Dictionary<string, string>
    {
        {PATHFINDER_1E, "https://www.d20pfsrd.com"},
        {DND_5E, "https://www.5esrd.com"},
    };

    public static Dictionary<string, string> GameSystemToPrefix = new Dictionary<string, string>
    {
        {PATHFINDER_1E, "d20pfsrd"},
        {DND_5E, "5esrd"},
    };

    public static Dictionary<string, string> GameSystemToTitlePostfix = new Dictionary<string, string>
    {
        {PATHFINDER_1E, " &#8211; d20PFSRD"},
        {DND_5E, " &#8211; 5th edition SRD"},
    };

    public static string ValidateSystem(string system)
    {
        system = system.ToLower();
        return system switch
        {
            PATHFINDER_1E => PATHFINDER_1E,
            DND_5E => DND_5E,
            _ => throw new ArgumentException("Not a valid Game System"),
        };
    }
}