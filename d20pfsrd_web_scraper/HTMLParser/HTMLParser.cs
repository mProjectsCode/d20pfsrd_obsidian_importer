using HtmlAgilityPack;

namespace d20pfsrd_web_scraper.HTMLParser;

public class HTMLParser
{
    public static void ParseHtml(string htmlString)
    {
        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(htmlString);
        
    }
}