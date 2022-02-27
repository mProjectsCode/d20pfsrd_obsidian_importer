using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using d20pfsrd_web_scraper.HTMLParser;

namespace d20pfsrd_web_scraper;

public class Note
{
    public string Title;
    public string WebTitle;
    public string FileName;
    public string LocalPathToFolder;
    public string LocalPathToHtml;
    public string LocalPathToJson;
    public string LocalPathToMarkdown;
    public string Url;
    public string TimeAccessed;
    public string[] Tags;

    public Note(string localPath, MdConverter mdConverter)
    {
        LocalPathToFolder = PathHelper.TrimSlashes(localPath);
        LocalPathToHtml = PathHelper.Combine(Program.InputFolder, localPath, "index.html");
        LocalPathToJson = PathHelper.Combine(Program.InputFolder, localPath, "meta.json");

        FileName = mdConverter.ConvertToMdTitle(localPath);
        Title = mdConverter.GetDocumentHeadingFromMdTitle(FileName);
        Tags = FileName.Split('_')[..^1];
        
        LocalPathToMarkdown = PathHelper.Combine(Program.OutputFolder, localPath, FileName + ".md");

        string meta = File.ReadAllText(Path.Combine(Program.RunLocation, LocalPathToJson));
        Page page = JsonSerializer.Deserialize<Page>(meta);
        
        WebTitle = page.Title;
        Url = page.URL;
        TimeAccessed = page.TimeAccessed.ToString();
    }

    public string ToMetadata()
    {
        return $@"---
title: {Title}
webTitle: {WebTitle}
fileName: {FileName}
localPathToFolder: {LocalPathToFolder}
localPathToHtml: {LocalPathToHtml}
localPathToJson: {LocalPathToJson}
localPathToMarkdown: {LocalPathToMarkdown}
url: {Url}
timeAccessed: {TimeAccessed}
tags: {TagsToMetadata()}
---
";
    }

    private string TagsToMetadata()
    {
        if (Tags.Length == 0)
        {
            return "";
        }
        
        StringBuilder sb = new StringBuilder();
        sb.Append("[ ");
        sb.Append(Tags[0]);

        for (int i = 1; i < Tags.Length; i++)
        {
            sb.Append(", ").Append(Tags[i]);
        }

        sb.Append(" ]");

        return sb.ToString();
    }
}