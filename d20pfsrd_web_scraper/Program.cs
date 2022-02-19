using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace d20pfsrd_web_scraper;

internal class Program
{
    public static string RunLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public static string OutputLocation = RunLocation + "/d20pfsrd/";
    public static HtmlWeb Web { get; set; } = new HtmlWeb();
    public static string ContentLinks { get; set; }
    public static string[] ContentLinksList { get; set; }

    public static string[] DomainBlackList { get; } =
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

    private static void Main(string[] args)
    {
        // CrawlSitemap();
        // CrawlPage("https://www.d20pfsrd.com/feats/general-feats/aboleth-deceiver/").Save();

        if (!File.Exists(RunLocation + "/contentLinks.txt"))
        {
            CrawlSitemap();
        }

        Console.WriteLine(RunLocation);
        
        ContentLinksList = File.ReadAllLines(RunLocation + "/contentLinks.txt");
        ContentLinks = File.ReadAllText(RunLocation + "/contentLinks.txt");

        MdConverter mdConverter = new MdConverter();

        if (true)
        {
            int i = 0;
            foreach (string contentLink in ContentLinksList)
            {
                Uri uri = new Uri(contentLink);
                string filePath = uri.AbsolutePath;
                Console.WriteLine($"{i} of {ContentLinksList.Length}");
                Console.WriteLine(filePath);
                string outPath = RunLocation + "/d20pfsrd_md" + filePath;

                try
                {
                    mdConverter.LoadAndConvert(OutputLocation + filePath, filePath, outPath);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not create md file");
                }

                i++;
            }

            i = 0;
            foreach (string contentLink in ContentLinksList)
            {
                Uri uri = new Uri(contentLink);
                string filePath = uri.AbsolutePath;
                Console.WriteLine($"Links {i} of {ContentLinksList.Length}");
                Console.WriteLine(filePath);
                string outPath = RunLocation + "/d20pfsrd_md" + filePath;

                try
                {
                    mdConverter.ConvertLinks(filePath, outPath);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could convert links");
                }

                i++;
            }
            
            File.WriteAllText(RunLocation + "/headingMap.json", JsonConvert.SerializeObject(mdConverter.Headings));
        }
        else
        {
            mdConverter.Headings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(RunLocation + "/headingMap.json"));
            Uri uri = new Uri("https://www.d20pfsrd.com/classes/core-classes/bard/");
            string filePath = uri.AbsolutePath;
            Console.WriteLine(filePath);
            string outPath = RunLocation + "/d20pfsrd_md" + filePath;
            mdConverter.LoadAndConvert(OutputLocation + filePath, filePath, outPath);
            mdConverter.ConvertLinks(filePath, outPath);
        }
    }

    private static void CrawlSitemap()
    {
        string url = "https://www.d20pfsrd.com/sitemap.xml.gz";

        List<string> subSitemapLinks = CrawlSubSitemaps(url);
        List<string> contentLinks = new List<string>();

        foreach (string subSitemapLink in subSitemapLinks)
        {
            Console.WriteLine(subSitemapLink);
            contentLinks.AddRange(CrawlSubSitemaps(subSitemapLink));
        }

        File.WriteAllLines(RunLocation + "contentLinks.txt", contentLinks.ToArray());

        List<string> failedLinks = new List<string>();

        int i = 0;

        foreach (string contentLink in contentLinks)
        {
            try
            {
                Console.WriteLine($"{i} of {contentLinks.Count}");
                Page page = CrawlPage(contentLink);
                page.Save();
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                failedLinks.Add(contentLink);
            }

            i++;
        }

        File.WriteAllLines("failedLinks.txt", failedLinks.ToArray());
    }

    private static List<string> CrawlSubSitemaps(string url)
    {
        WebClient wc = new WebClient();
        wc.Encoding = Encoding.UTF8;
        string sitemapString = wc.DownloadString(url);

        // Console.WriteLine(sitemapString);

        string pattern = @"<loc.*>(.*?)<\/loc>";
        Regex regex = new Regex(pattern);
        MatchCollection matchCollection = regex.Matches(sitemapString);

        List<string> links = new List<string>();

        foreach (Match match in matchCollection)
        {
            string a = match.Value.Substring(5);
            a = a.Remove(a.Length - 6);

            bool filter = false;
            foreach (string s in DomainBlackList)
            {
                if (a.Contains(s))
                {
                    filter = true;
                }
            }

            if (!filter)
            {
                links.Add(a);
            }
        }

        return links;
    }

    private static Page CrawlPage(string url)
    {
        Web.OverrideEncoding = Encoding.UTF8;
        HtmlDocument document = Web.Load(url);
        HtmlNodeCollection htmlNodeCollection = document.DocumentNode.SelectNodes("//div[@class='article-content']");
        string title = document.DocumentNode.SelectSingleNode("html/head/title").InnerText;

        if (htmlNodeCollection.Count > 0)
        {
            return new Page(title, url, htmlNodeCollection[0].InnerHtml, DateTime.Now);
        }

        return new Page(title, url, "", DateTime.Now);
    }
}

internal class Page
{
    public Page(string title, string url, string content, DateTime timeAccessed)
    {
        string titleFilter = " &#8211; d20PFSRD";
        if (title.Contains(titleFilter))
        {
            title = title.Remove(title.Length - titleFilter.Length);
        }

        Title = title;
        URL = url;
        Content = content;
        TimeAccessed = timeAccessed;

        // File.WriteAllLines("test.html", new string[] { content });
    }

    public string Title { get; set; }
    public string URL { get; set; }

    [JsonIgnore] public string Content { get; set; }

    public DateTime TimeAccessed { get; set; }

    public void Save()
    {
        Uri uri = new Uri(URL);
        DirectoryInfo directoryInfo = Directory.CreateDirectory(Program.OutputLocation + uri.AbsolutePath);
        Console.WriteLine(directoryInfo.FullName);
        File.WriteAllText(directoryInfo.FullName + "index.html", Content);
        File.WriteAllText(directoryInfo.FullName + "meta.json", JsonConvert.SerializeObject(this));
    }
}