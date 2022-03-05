namespace d20pfsrd_web_scraper;

public class PathHelper
{
    public static string Combine(string path1, params string[] paths)
    {
        if (path1 == "")
        {
            throw new ArgumentException("The path was empty");
        }

        if (paths.Length == 0)
        {
            throw new ArgumentException("Can not combine a path with nothing");
        }

        path1 = Combine(path1, paths[0]);

        if (paths.Length == 1)
        {
            return path1;
        }

        return Combine(path1, paths[1..]);
    }

    public static string Combine(string path1, string path2)
    {
        path1 = TrimSlashes(path1);
        path2 = TrimSlashes(path2);

        return path1 + "/" + path2;
    }

    public static string TrimSlashes(string path)
    {
        path = ConvertSlashes(path);

        if (path.EndsWith("/"))
        {
            path = path[..^1];
        }

        if (path.StartsWith("/"))
        {
            path = path[1..];
        }

        return path;
    }

    public static string ConvertSlashes(string path)
    {
        path = path.Replace("\\", "/");

        return path;
    }
    
    public static string ConvertMdTitleToPath(string mdTitle)
    {
        string[] a = mdTitle.Split('_');

        return string.Join('/', a[..^1]);
    }
    
    public static string GetName(string path)
    {
        path = TrimSlashes(path);
        
        string[] a = path.Split('/');

        return a[^1];
    }
}