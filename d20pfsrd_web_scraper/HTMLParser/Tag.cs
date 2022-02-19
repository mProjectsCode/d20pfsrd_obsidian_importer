namespace d20pfsrd_web_scraper.HTMLParser;

public class Tag
{
    public string OuterHtml { get; set; }
    public string InnerHtml { get; set; }

    public string Type;
    public Dictionary<string, string> Fields;
    
    public List<Tag> Children;
}