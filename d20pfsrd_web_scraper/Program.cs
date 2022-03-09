using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace d20pfsrd_web_scraper;

internal class Program
{
    private const bool ParseAll = true;

    public static string System = "";

    // Input folder of the scraped HTML
    public static string HtmlFolder = "";

    // Output folder of the parsed Markdown
    public static string MarkdownFolder = "";
    public static string ContentLinksFileName = "";
    public static string FailedLinksFileName = "";
    public static string HeadingMapFileName = "";
    public static string FileOverridesFolderName = "";

    public static string ScraperOutputLocation = "";
    public static readonly string RunLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";


    public static readonly ILoggerFactory LFactory = LoggerFactory.Create(builder =>
    {
        builder.AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("d20pfsrd_web_scraper", LogLevel.Debug)
            .AddConsole();
    });

    public static Config? Config;

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
        if (RunLocation == "")
        {
            Console.WriteLine("Run location is null. The script will exit");
        }
        
        
        Console.WriteLine("---");
        Console.WriteLine("d20pfsrd obsidian importer");
        Console.WriteLine("---");

        Console.WriteLine($"Run location: {RunLocation}");
        Console.WriteLine("---");


        Config = new Config();

        try
        {
            Config.ReadConfig();
        }
        catch (Exception e) when (e is SerializationException or FormatException)
        {
            Console.WriteLine("ERROR: Could not read config");
            Console.WriteLine(e.Message);
            return;
        }

        Console.WriteLine("Loaded Config");
        Console.WriteLine("---");

        try
        {
            System = GameSystem.ValidateSystem(Config.Options.GameSystem);
            Console.WriteLine($"Converting SRD for {System}");
            Console.WriteLine("---");
        }
        catch (Exception e) when (e is ArgumentException)
        {
            Console.WriteLine("ERROR: System invalid");
            Console.WriteLine(e.Message);
            return;
        }


        HtmlFolder = GameSystem.GameSystemToPrefix[System] + "_html";
        MarkdownFolder = GameSystem.GameSystemToPrefix[System] + "_md";
        ContentLinksFileName = GameSystem.GameSystemToPrefix[System] + "_contentLinks.txt";
        FailedLinksFileName = GameSystem.GameSystemToPrefix[System] + "_failedLinks.txt";
        HeadingMapFileName = GameSystem.GameSystemToPrefix[System] + "_headingMap.json";
        FileOverridesFolderName = GameSystem.GameSystemToPrefix[System] + "_overrides";
        ScraperOutputLocation = PathHelper.Combine(RunLocation, HtmlFolder);

        if (!File.Exists(PathHelper.Combine(RunLocation, ContentLinksFileName)))
        {
            Console.WriteLine("Crawling sitemap...");
            SrdCrawler srdCrawler = new SrdCrawler();
            srdCrawler.CrawlSitemap();
        }

        ContentLinksList = File.ReadAllLines(PathHelper.Combine(RunLocation, ContentLinksFileName));
        Console.WriteLine("Loaded content links");
        Console.WriteLine("---");

        // init the markdown converter
        MdConverter.Init();

        if (ParseAll)
        {
            if (!Config.Options.SkipParsing)
            {
                if (Config.Options.ParseAsync)
                {
                    ParseAllAsync();
                }
                else
                {
                    ParseAllSync();
                }

                File.WriteAllText(PathHelper.Combine(RunLocation, HeadingMapFileName), JsonConvert.SerializeObject(MdConverter.Headings));
            }
            else
            {
                Console.WriteLine("Skipped parsing the vault");
            }

            ResolveFileOverrides();
        }
        else
        {
            ParseTest("https://www.d20pfsrd.com/classes/core-classes/druid/");
        }
    }

    private static void ResolveFileOverrides()
    {
        Console.WriteLine("---");
        Console.WriteLine("Copying file overrides");
        Console.WriteLine("---");

        string fileOverridesPath = PathHelper.Combine(RunLocation, FileOverridesFolderName);

        if (!Directory.Exists(fileOverridesPath))
        {
            Console.WriteLine("There is not directory to copy from");
            return;
        }

        foreach (string file in Directory.GetFiles(fileOverridesPath))
        {
            string fileName = PathHelper.GetName(file);


            if (fileName.StartsWith('_'))
            {
                continue;
            }

            string filePath = PathHelper.ConvertMdTitleToPath(fileName);
            // filePath = PathHelper.Combine(filePath, fileName);

            string copyToFolder = PathHelper.Combine(RunLocation, MarkdownFolder, filePath);
            Directory.CreateDirectory(copyToFolder);
            string copyToPath = PathHelper.Combine(copyToFolder, fileName);

            Console.WriteLine($"copying {fileName} to {copyToPath}");

            File.Copy(file, copyToPath, true);
        }

        Console.WriteLine("---");
        Console.WriteLine("Successfully copied file overrides");
        Console.WriteLine("---");
    }

    private static void ParseTest(string url)
    {
        Console.WriteLine($"Converting Test file: {url}");
        Console.WriteLine("---");

        MdConverter.Headings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(PathHelper.Combine(RunLocation, HeadingMapFileName)));
        Uri uri = new Uri(url);
        string filePath = uri.AbsolutePath;

        NoteMetadata noteMetadata = new NoteMetadata(filePath, PathMappings.GetMapping(filePath));
        Directory.CreateDirectory(PathHelper.Combine(RunLocation, MarkdownFolder, noteMetadata.LocalPathToFolder));

        Console.WriteLine("Note Metadata: ");
        Console.WriteLine(JsonConvert.SerializeObject(noteMetadata, Formatting.Indented));

        string md = MdConverter.LoadAndConvert(noteMetadata);
        File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);

        md = MdConverter.ConvertLinks(noteMetadata);
        File.WriteAllText(Path.Combine(RunLocation, noteMetadata.LocalPathToMarkdown), md);
    }

    private static void ParseAllSync()
    {
        Console.WriteLine("Converting all in sync");
        Console.WriteLine("---");

        int i = 0;
        foreach (string contentLink in ContentLinksList)
        {
            Uri uri = new Uri(contentLink);
            string filePath = uri.AbsolutePath;
            Console.WriteLine($"{i} of {ContentLinksList.Length}");
            // Console.WriteLine(filePath);

            if (File.Exists(PathHelper.Combine(RunLocation, HtmlFolder, filePath, "index.html")))
            {
                NoteMetadata noteMetadata = new NoteMetadata(filePath, PathMappings.GetMapping(filePath));

                try
                {
                    string md = MdConverter.LoadAndConvert(noteMetadata);
                    Directory.CreateDirectory(PathHelper.Combine(RunLocation, MarkdownFolder, noteMetadata.LocalPathToFolder));
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

            if (File.Exists(PathHelper.Combine(RunLocation, HtmlFolder, filePath, "index.html")))
            {
                NoteMetadata noteMetadata = new NoteMetadata(filePath, PathMappings.GetMapping(filePath));

                try
                {
                    string md = MdConverter.ConvertLinks(noteMetadata);
                    Directory.CreateDirectory(PathHelper.Combine(RunLocation, MarkdownFolder, noteMetadata.LocalPathToFolder));
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
        Console.WriteLine("Converting all async");
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

                    if (!File.Exists(PathHelper.Combine(RunLocation, HtmlFolder, filePath, "index.html")))
                    {
                        return new TaskRetObj(false);
                    }

                    NoteMetadata noteMetadata = new NoteMetadata(filePath, PathMappings.GetMapping(filePath));

                    try
                    {
                        return new TaskRetObj(noteMetadata, MdConverter.LoadAndConvert(noteMetadata));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Could not create md file");
                        Console.WriteLine(e.Message);
                        return new TaskRetObj(false);
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

                Directory.CreateDirectory(PathHelper.Combine(RunLocation, MarkdownFolder, taskRetObj.NoteMetadata.LocalPathToFolder));
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

                    if (!File.Exists(PathHelper.Combine(RunLocation, HtmlFolder, filePath, "index.html")))
                    {
                        return new TaskRetObj(false);
                    }

                    NoteMetadata noteMetadata = new NoteMetadata(filePath, PathMappings.GetMapping(filePath));

                    try
                    {
                        return new TaskRetObj(noteMetadata, MdConverter.ConvertLinks(noteMetadata));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could convert links");
                        return new TaskRetObj(false);
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

                Directory.CreateDirectory(PathHelper.Combine(RunLocation, MarkdownFolder, taskRetObj.NoteMetadata.LocalPathToFolder));
                File.WriteAllText(Path.Combine(RunLocation, taskRetObj.NoteMetadata.LocalPathToMarkdown), taskRetObj.Md);
            }
        }
    }
}