namespace d20pfsrd_web_scraper;

public class PathMappings
{
    // this works on a local folderpath without the file name so e.g "classes/core-classes/fighter"
    public static string GetMapping(string path)
    {
        path = PathHelper.TrimSlashes(path);
        List<string> pathParts = path.Split('/').ToList();

        return Program.System switch
        {
            GameSystem.PATHFINDER_1E => GetMappingD20pfsrd(pathParts),
            GameSystem.DND_5E => GetMapping5esrd(pathParts),
            _ => path,
        };
    }

    private static string GetMappingD20pfsrd(List<string> path)
    {
        // remove sub folders for spells
        int allSpellsIndex = path.IndexOf("all-spells");
        if (allSpellsIndex != -1 && allSpellsIndex + 1 < path.Count)
        {
            path.RemoveAt(allSpellsIndex + 1);
        }

        return string.Join('/', path);
    }

    private static string GetMapping5esrd(List<string> path)
    {
        return string.Join('/', path);
    }
}