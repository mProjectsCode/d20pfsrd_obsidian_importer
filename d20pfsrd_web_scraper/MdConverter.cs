using System.Text;
using System.Text.RegularExpressions;

namespace d20pfsrd_web_scraper;

public class MdConverter
{
    public Dictionary<string, List<string>> Headings;

    public MdConverter()
    {
        Headings = new Dictionary<string, List<string>>(Program.ContentLinksList.Length);
        foreach (string contentLink in Program.ContentLinksList)
        {
            Uri hrefUri = new Uri(contentLink);
            string hrefPath = hrefUri.AbsolutePath;
            
            Headings[ConvertToMdTitle(hrefPath)] = new List<string>();
        }
    }

    public void LoadAndConvert(string path, string relPath, string outPath)
    {
        Console.WriteLine(outPath);
        string html = File.ReadAllText(path + "index.html", Encoding.UTF8);
        ConvertToMd(html, path, relPath, outPath);
    }

    public void ConvertToMd(string html, string path, string relPath, string outPath)
    {
        string title = ConvertToMdTitle(relPath);

        string markdown = "# " + GetDocumentHeadingFromMdTitle(title) + "\n";

        markdown += ParseHtml(html, title);

        //Directory.CreateDirectory(outPath);

        File.WriteAllText(outPath + title + ".md", markdown, Encoding.UTF8);
    }

    public void ConvertLinks(string relPath, string outPath)
    {
        Console.WriteLine(outPath);
        string title = ConvertToMdTitle(relPath);
        string document = File.ReadAllText(outPath + title + ".md", Encoding.UTF8);

        document = UpdateLinks(document);
        
        File.WriteAllText(outPath + title + ".md", document, Encoding.UTF8);
    }

    public string ParseHtml(string html, string title)
    {
        html = html.Trim();
        
        html = html.Replace("\r\n", "\n");
        html = html.Replace("\r", "\n");

        html = Regex.Replace(html, @"<!--.*?>", "");
        
        html = Regex.Replace(html, @"</?p[^>]*>", "\n");

        html = Regex.Replace(html, @"</?span[^>]*>", "");

        html = Regex.Replace(html, @"</?div[^>]*>", "");

        html = Regex.Replace(html, @"<h1[^>]*>", "\n# ");
        html = Regex.Replace(html, @"</h1[^>]*>", "");

        html = Regex.Replace(html, @"<h2[^>]*>", "\n## ");
        html = Regex.Replace(html, @"</h2[^>]*>", "");

        html = Regex.Replace(html, @"<h3[^>]*>", "\n### ");
        html = Regex.Replace(html, @"</h3[^>]*>", "");

        html = Regex.Replace(html, @"<h4[^>]*>", "\n#### ");
        html = Regex.Replace(html, @"</h4[^>]*>", "");

        html = Regex.Replace(html, @"<h5[^>]*>", "\n##### ");
        html = Regex.Replace(html, @"</h5[^>]*>", "");

        html = Regex.Replace(html, @"<h6[^>]*>", "\n###### ");
        html = Regex.Replace(html, @"</h6[^>]*>", "");


        html = Regex.Replace(html, @" *<i> *", " *");
        html = Regex.Replace(html, @" *</i> *", "* ");

        html = Regex.Replace(html, @" *<b> *", " **");
        html = Regex.Replace(html, @" *</b> *", "** ");

        html = Regex.Replace(html, @"</?ul[^>]*>", "");

        html = Regex.Replace(html, @"<li[^>]*>", " - ");
        html = Regex.Replace(html, @"</li>", "\n");

        html = Regex.Replace(html, @"<a (name|id)[^>]*></a>", "");

        html = Regex.Replace(html, @"<br>", "\n");
        
        // html = html.Replace("\r", "\n");
        html = Regex.Replace(html, "\n\n\n+", "\n\n");
        
        // html = Regex.Replace(html, "\n#", "\n\n#");
        //html = Regex.Replace(html, "\n<", "\n\n<");

        MatchCollection headings = Regex.Matches(html, "#+? .+?\n");
        foreach (Match heading in headings)
        {
            string v = heading.Value;
            v = Regex.Replace(v, "#+? ", "");
            v = Regex.Replace(v, "\n", "");
            v = v.Trim();
            Headings[title].Add(v);
        }

        return html;
    }

    public string ConvertToMdTitle(string relPath)
    {
        string title = relPath.Replace('/', '_');
        title = title.Replace('\\', '_');

        int hashtagIndex = title.IndexOf('#');
        string subTitle = "";
        if (hashtagIndex > 0)
        {
            // Console.WriteLine(title.Length);
            // Console.WriteLine(hashtagIndex);
            subTitle = title.Substring(hashtagIndex, title.Length - hashtagIndex);
            subTitle = subTitle.Replace("TOC-", "");
            subTitle = subTitle.Replace('-', ' ');
            title = title.Remove(hashtagIndex);
        }

        if (title.EndsWith('_'))
        {
            title = title.Remove(title.Length - 1);
        }

        if (title.StartsWith('_'))
        {
            title = title[1..];
        }

        return (title + subTitle).Trim();
    }

    public string GetDocumentHeadingFromMdTitle(string mdTitle)
    {
        string[] parts = mdTitle.Split('_');
        return FirstCharToUpper(parts[parts.Length - 1]);
    }

    public string FirstCharToUpper(string input)
    {
        return input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input[0].ToString().ToUpper() + input.Substring(1),
        };
    }

    public string UpdateLinks(string html)
    {
        // get all a tags
        MatchCollection links = Regex.Matches(html, @"<a[^>]*>.*?</a>");

        List<string> replacementList = new List<string>();
        List<int> originalLength = new List<int>();
        List<int> indices = new List<int>();

        string linksAtEndOfFile = "";
        string footer = "";
        int footerIndex = 1;

        foreach (Match match in links)
        {
            // Console.WriteLine(match.Value);

            // get the href
            Match hrefMatch = Regex.Match(match.Value, " href=\".+?\"");
            if (!hrefMatch.Success)
            {
                continue;
            }

            string href = hrefMatch.Value;
            // remove the ' href='
            href = href.Replace(" href=", "");
            // remove the '"'
            href = href.Replace("\"", "");

            // remove the opening and closing <a ...> tag
            string content = Regex.Replace(match.Value, @"<a[^>]*>", "");
            content = Regex.Replace(content, @"</a>", "");
            Console.WriteLine(href);

            // Console.WriteLine(nodeHref);
            try
            {
                Uri hrefUri = new Uri(href);
                string hrefPath = hrefUri.AbsolutePath + hrefUri.Fragment;
                bool hasFragment = false;
                string hrefWoFragment = href;
                if (hrefWoFragment.IndexOf("#") >= 0)
                {
                    hasFragment = true;
                    hrefWoFragment = hrefWoFragment[..hrefWoFragment.IndexOf("#")];
                }

                if (!hrefWoFragment.EndsWith("/"))
                {
                    hrefWoFragment += "/";
                }

                Console.WriteLine(hrefPath);
                Console.WriteLine(hrefWoFragment);
                
                // look if it is an internal link
                bool isInternalLink = false;
                foreach (string contentLink in Program.ContentLinksList)
                {
                    if (hrefWoFragment == contentLink)
                    {
                        isInternalLink = true;
                    }
                }

                if (!isInternalLink)
                {
                    Console.WriteLine("not an internal link");
                    continue;
                }

                bool foundTOCItem = false;
                string TOCItem = "";
                if (hasFragment)
                {
                    foreach (string heading in Headings[ConvertToMdTitle(hrefUri.AbsolutePath)])
                    {
                        if (heading == hrefUri.Fragment)
                        {
                            foundTOCItem = true;
                            TOCItem = heading;
                        }
                    }

                    /*
                    if (!foundTOCItem)
                    {
                        foreach (string heading in Headings[hrefWoFragment])
                        {
                            if (heading.Contains(hrefUri.Fragment))
                            {
                                foundTOCItem = true;
                                TOCItem = heading;
                            }
                        }
                    }
                    */
                }
                
                // find out weather 
                string subHtml = html.Substring(match.Index);
                subHtml = Regex.Replace(subHtml, @"<img [^>]+?>", "");
                MatchCollection openingTags = Regex.Matches(subHtml, @"<[^(/|!)][^>]*?>");
                MatchCollection closingTags = Regex.Matches(subHtml, @"</[^(>|\-)]*?>");
                // Console.WriteLine(subHtml.Remove(400));
                // Console.WriteLine(openingTags.Count);
                // Console.WriteLine(closingTags.Count);
                
                string mdTitle = ConvertToMdTitle(hrefPath);

                if (foundTOCItem)
                {
                    mdTitle = RemoveFragment(mdTitle);
                    mdTitle += "#" + TOCItem;

                    footer += $"[^{footerIndex}]: TOC-Item: {TOCItem} \nd20pfsrd-Link: {href}\n";
                }

                string linkText = "";
                // not in another element
                if (openingTags.Count == closingTags.Count)
                {
                    linkText = $"[[{mdTitle}|{content}]]";

                    if (foundTOCItem)
                    {
                        linkText += $"[^{footerIndex}]";
                    }
                    
                    Console.WriteLine("replaced href with wikilink");
                }
                // in another element
                else if (openingTags.Count < closingTags.Count)
                {
                    linkText = $"<a data-href=\"{mdTitle}\" href=\"{mdTitle}\" class=\"internal-link\" target=\"_blank\" rel=\"noopener\">{content}</a>";
                    linksAtEndOfFile += "\n" + $"[[{mdTitle}|{content}]]";
                    
                    if (foundTOCItem)
                    {
                        linkText += $"[^{footerIndex}]";
                    }

                    Console.WriteLine("replaced href a");
                }
                replacementList.Add(linkText);
                
                originalLength.Add(match.Value.Length);
                indices.Add(match.Index);

                footerIndex += 1;
            }
            catch (UriFormatException)
            {
                Console.WriteLine("cant read href");
            }
        }

        string[] splitHtml = SplitAt(html, indices.ToArray());
        string output = "";

        for (int i = 0; i < splitHtml.Length; i++)
        {
            if (i == 0)
            {
                output = splitHtml[i];
                continue;
            }

            // remove the old link
            // Console.WriteLine(splitHtml[i].Length + " - " + origionalLength[i]);
            string part = splitHtml[i].Substring(originalLength[i - 1]);
            // add new one
            output += replacementList[i - 1] + part;
        }

        output += "\n\n" + linksAtEndOfFile;
        
        output += "\n\n" + footer;

        return output;
    }

    public string[] SplitAt(string source, params int[] index)
    {
        index = index.Distinct().OrderBy(x => x).ToArray();
        string[] output = new string[index.Length + 1];
        int pos = 0;

        for (int i = 0; i < index.Length; pos = index[i++])
        {
            output[i] = source[pos..index[i]];
        }

        output[index.Length] = source[pos..];
        return output;
    }

    private string RemoveFragment(string s)
    {
        if (s.IndexOf("#") >= 0)
        {
            s = s[..s.IndexOf("#")];
        }
        
        return s;
    }

    public struct Tag
    {
        public int Index { get; set; }
        public bool Opening { get; set; }

        public Tag(int index, bool opening)
        {
            Index = index;
            Opening = opening;
        }
    }
}