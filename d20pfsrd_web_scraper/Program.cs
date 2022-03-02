using System.Reflection;
using Newtonsoft.Json;

namespace d20pfsrd_web_scraper;

internal class Program
{
    // Experimental, i dont think it is faster but not tested
    private const bool UseMultithreading = true; 
    // Weather to parse all file or just a test file
    private const bool ParseAll = true; 

    // Input folder of the scraped HTML
    public const string InputFolder = "d20pfsrd";
    // Output folder of the parsed Markdown
    public const string OutputFolder = "d20pfsrd_md";
    public static readonly string RunLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static readonly string ScraperOutputLocation = PathHelper.Combine(RunLocation, "d20pfsrd");

    // A blacklist of domain that we do not want to scrap
    public static readonly string[] DomainBlackList =
    {
        "/wp-json",
        "/wp-admin",
        "/wp-includes",
        "/trackback",
        "/wp-login",
        "/wp-register",
        "/staging",
        "/staging__trashed",
        "/work-area",
        "/extras",
    };

    // List of all files crawled
    public static string[]? ContentLinksList { get; private set; }

    private static void Main(string[] args)
    {
        Console.WriteLine("---");
        Console.WriteLine("d20pfsrd obsidian importer");
        Console.WriteLine("---");

        if (!File.Exists(PathHelper.Combine(RunLocation, "contentLinks.txt")))
        {
            Console.WriteLine("Crawling sitemap...\n");
            D20pfsrdCrawler d20pfsrdCrawler = new D20pfsrdCrawler();
            d20pfsrdCrawler.CrawlSitemap();
        }

        ContentLinksList = File.ReadAllLines(PathHelper.Combine(RunLocation, "contentLinks.txt"));
        Console.WriteLine("Loaded content links");
        Console.WriteLine("---");

        // init the markdown converter
        MdConverter.Init();

        if (ParseAll)
        {
            if (UseMultithreading)
            {
                ParseAllAsync();
            }
            else
            {
                ParseAllSync();
            }

            File.WriteAllText(PathHelper.Combine(RunLocation, "headingMap.json"), JsonConvert.SerializeObject(MdConverter.Headings));
        }
        else
        {
            ParseTest("https://www.d20pfsrd.com/classes/");
        }
        
    }

    private static void ParseTest(string url)
    {
        Console.WriteLine($"Converting Test file: {url}");
        Console.WriteLine("---");
        
        MdConverter.Headings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(RunLocation + "/headingMap.json"));
        Uri uri = new Uri(url);
        string filePath = uri.AbsolutePath;

        NoteMetadata noteMetadata = new NoteMetadata(filePath);
        Directory.CreateDirectory(PathHelper.Combine(RunLocation, OutputFolder, noteMetadata.LocalPathToFolder));

        Console.WriteLine("Note Metadata: ");
        Console.WriteLine(JsonConvert.SerializeObject(noteMetadata, Formatting.Indented));

        string md = MdConverter.LoadAndConvert(noteMetadata);
        File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);

        md = MdConverter.ConvertLinks(noteMetadata);
        File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);
    }

    private static void ParseAllSync()
    {
        Console.WriteLine($"Converting all in sync");
        Console.WriteLine("---");
        
        int i = 0;
        foreach (string contentLink in ContentLinksList)
        {
            Uri uri = new Uri(contentLink);
            string filePath = uri.AbsolutePath;
            Console.WriteLine($"{i} of {ContentLinksList.Length}");
            // Console.WriteLine(filePath);

            if (File.Exists(PathHelper.Combine(RunLocation, InputFolder, filePath, "index.html")))
            {
                NoteMetadata noteMetadata = new NoteMetadata(filePath);

                try
                {
                    string md = MdConverter.LoadAndConvert(noteMetadata);
                    Directory.CreateDirectory(PathHelper.Combine(RunLocation, OutputFolder, noteMetadata.LocalPathToFolder));
                    File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not create md file");
                }
            }

            i++;
        }

        i = 0;
        foreach (string contentLink in ContentLinksList)
        {
            Uri uri = new Uri(contentLink);
            string filePath = uri.AbsolutePath;
            Console.WriteLine($"Links {i} of {ContentLinksList.Length}");
            // Console.WriteLine(filePath);

            if (File.Exists(PathHelper.Combine(RunLocation, InputFolder, filePath, "index.html")))
            {
                NoteMetadata noteMetadata = new NoteMetadata(filePath);

                try
                {
                    string md = MdConverter.ConvertLinks(noteMetadata);
                    Directory.CreateDirectory(PathHelper.Combine(RunLocation, OutputFolder, noteMetadata.LocalPathToFolder));
                    File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could convert links");
                }
            }

            i++;
        }
    }


    private static void ParseAllAsync()
    {
        Console.WriteLine($"Converting all async");
        Console.WriteLine("---");
        
        const int batchSize = 1000;
        int numberOfBatches = (int) Math.Ceiling(ContentLinksList.Length / (double) batchSize);

        for (int i = 0; i < numberOfBatches; i++)
        {
            List<Task<TaskRetObj>> tasks = new List<Task<TaskRetObj>>(batchSize);

            for (int j = 0; j < batchSize; j++)
            {
                int j1 = j;
                int i1 = i;
                tasks.Add(Task.Run(() =>
                {
                    int num = i1 * batchSize + j1;
                    if (num >= ContentLinksList.Length)
                    {
                        return new TaskRetObj(false);
                    }

                    string contentLink = ContentLinksList[num];

                    Uri uri = new Uri(contentLink);
                    string filePath = uri.AbsolutePath;
                    // Console.WriteLine($"{num} of {ContentLinksList.Length}");
                    // Console.WriteLine(filePath);

                    if (!File.Exists(PathHelper.Combine(RunLocation, InputFolder, filePath, "index.html")))
                    {
                        return new TaskRetObj(false);
                    }

                    NoteMetadata noteMetadata = new NoteMetadata(filePath);

                    try
                    {
                        return new TaskRetObj(noteMetadata, MdConverter.LoadAndConvert(noteMetadata));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could not create md file");
                        return new TaskRetObj(false);
                        ;
                    }
                }));
            }

            Console.WriteLine($"Writing batch {i}");
            foreach (Task<TaskRetObj> task in tasks)
            {
                TaskRetObj taskRetObj = task.Result;
                if (!taskRetObj.Success || taskRetObj.NoteMetadata == null)
                {
                    continue;
                }

                Directory.CreateDirectory(PathHelper.Combine(RunLocation, OutputFolder, taskRetObj.NoteMetadata.LocalPathToFolder));
                File.WriteAllText(Path.Combine(RunLocation, taskRetObj.NoteMetadata.LocalPathToMarkdown), taskRetObj.Md);
            }
        }

        for (int i = 0; i < numberOfBatches; i++)
        {
            List<Task<TaskRetObj>> tasks = new List<Task<TaskRetObj>>(batchSize);

            for (int j = 0; j < batchSize; j++)
            {
                int i1 = i;
                int j1 = j;
                tasks.Add(Task.Run(() =>
                {
                    int num = i1 * batchSize + j1;
                    if (num >= ContentLinksList.Length)
                    {
                        return new TaskRetObj(false);
                    }

                    string contentLink = ContentLinksList[num];

                    Uri uri = new Uri(contentLink);
                    string filePath = uri.AbsolutePath;
                    Console.WriteLine($"{num} of {ContentLinksList.Length}");
                    // Console.WriteLine(filePath);

                    if (!File.Exists(PathHelper.Combine(RunLocation, InputFolder, filePath, "index.html")))
                    {
                        return new TaskRetObj(false);
                    }

                    NoteMetadata noteMetadata = new NoteMetadata(filePath);

                    try
                    {
                        return new TaskRetObj(noteMetadata, MdConverter.ConvertLinks(noteMetadata));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could convert links");
                        return new TaskRetObj(false);
                        ;
                    }
                }));
            }

            Console.WriteLine($"Writing batch {i}");
            foreach (Task<TaskRetObj> task in tasks)
            {
                TaskRetObj taskRetObj = task.Result;
                if (!taskRetObj.Success || taskRetObj.NoteMetadata == null)
                {
                    continue;
                }

                Directory.CreateDirectory(PathHelper.Combine(RunLocation, OutputFolder, taskRetObj.NoteMetadata.LocalPathToFolder));
                File.WriteAllText(Path.Combine(RunLocation, taskRetObj.NoteMetadata.LocalPathToMarkdown), taskRetObj.Md);
            }
        }
    }
}