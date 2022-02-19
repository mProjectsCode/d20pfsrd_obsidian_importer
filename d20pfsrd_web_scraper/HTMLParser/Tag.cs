namespace d20pfsrd_web_scraper.HTMLParser;

public class Tag
{
    public List<Tag> Children;
    public Dictionary<string, string> Fields;

    public string Type;
    public string OuterHtml { get; set; }
    public string InnerHtml { get; set; }
}