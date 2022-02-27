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

    public void LoadAndConvert(Note note)
    {
        Console.WriteLine(PathHelper.Combine(Program.RunLocation, note.LocalPathToHtml));
        
        string html = File.ReadAllText(PathHelper.Combine(Program.RunLocation, note.LocalPathToHtml), Encoding.UTF8);
        ConvertToMd(html, note);
    }

    public void ConvertToMd(string html, Note note)
    {
        // Console.WriteLine(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown));
        
        string markdown = note.ToMetadata() + "\n";
        markdown += "# " + note.Title + "\n";
        markdown += ParseHtml(html, note.FileName);
        
        Directory.CreateDirectory(PathHelper.Combine(Program.RunLocation, Program.OutputFolder, note.LocalPathToFolder));

        File.WriteAllText(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown), markdown, Encoding.UTF8);
    }

    public void ConvertLinks(Note note)
    {
        Console.WriteLine(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown));
        
        string document = File.ReadAllText(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown), Encoding.UTF8);

        document = UpdateLinks(document, note.FileName);

        File.WriteAllText(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown), document, Encoding.UTF8);
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

        html = Regex.Replace(html, @"</?ul[^>]*>", "\n");

        html = Regex.Replace(html, @"<li[^>]*>", " - ");
        html = Regex.Replace(html, @"</li>", "\n");

        html = Regex.Replace(html, @"<a (name|id)[^>]*></a>", "");

        html = Regex.Replace(html, @"<br>", "\n");
        html = Regex.Replace(html, @"<hr>", "\n---\n");

        html = Regex.Replace(html, @"\* :", "*:");

        html = Regex.Replace(html, "\n +?<", "\n<");

        html = html.Replace("\r", "\n");
        html = Regex.Replace(html, "\n\n\n+", "\n\n");

        // html = Regex.Replace(html, "\n#", "\n\n#");

        html = Regex.Replace(html, "\n\n<", "\n<");
        html = Regex.Replace(html, "\n\n - ", "\n - ");

        html = Regex.Replace(html, "&amp;", "&");
        html = Regex.Replace(html, "&quot;", "\"");
        html = Regex.Replace(html, "&#039;", "\'");
        html = Regex.Replace(html, "&apos;", "\'");
        html = Regex.Replace(html, "&#8217;", "\'");

        MatchCollection headings = Regex.Matches(html, "#+? .+?\n");
        foreach (Match heading in headings)
        {
            string v = heading.Value;
            v = Regex.Replace(v, "#+? ", "");
            v = Regex.Replace(v, "\n", "");
            v = v.Trim();
            v = Regex.Replace(v, @"<a[^>]*>", "");
            v = Regex.Replace(v, @"</a>", "");
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
        if (hashtagIndex != -1)
        {
            subTitle = title.Substring(hashtagIndex, title.Length - hashtagIndex);
            subTitle = subTitle.Replace("TOC-", "");
            subTitle = subTitle.Replace("toc-", "");
            if (int.TryParse(subTitle.Substring(1), out _))
            {
                subTitle = "";
            }

            subTitle = subTitle.Replace('-', ' ');
            subTitle = subTitle.Replace('_', ' ');
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
        if (mdTitle == "")
        {
            mdTitle = "index";
        }
        
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

    public string RemoveFragment(string path)
    {
        int hashtagIndex = path.IndexOf('#');
        if (hashtagIndex == -1)
        {
            return path;
        }

        return path.Remove(hashtagIndex);
    }

    public string UpdateLinks(string html, string title)
    {
        // get all a tags
        MatchCollection links = Regex.Matches(html, @"<a[^>]*>.*?</a>");

        List<LinkReplacement> replacements = new List<LinkReplacement>();

        StringBuilder linksAtEndOfFile = new StringBuilder();
        StringBuilder footer = new StringBuilder();
        int footerIndex = 1;

        foreach (Match match in links)
        {
            // Console.WriteLine();

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

            // Console.WriteLine(href);

            if (!(href.StartsWith("https://www.d20pfsrd.com") || href.StartsWith("#")))
            {
                continue;
            }

            // Console.WriteLine(nodeHref);
            try
            {
                string hrefPath = "";
                string woFragmentPath = "";
                bool isSelfLink = false;
                bool hasFragment = false;
                bool isInternalLink = false;
                string fragment = "";

                if (href.StartsWith('#'))
                {
                    hrefPath = href;
                    woFragmentPath = href;
                    isSelfLink = true;
                    hasFragment = true;
                    isInternalLink = true;
                }
                else
                {
                    Uri hrefUri = new Uri(href);
                    hrefPath = hrefUri.AbsolutePath + hrefUri.Fragment;
                    woFragmentPath = RemoveFragment(hrefPath);
                }

                if (href.Contains('#'))
                {
                    hasFragment = true;
                }

                if (!woFragmentPath.EndsWith("/"))
                {
                    woFragmentPath += "/";
                }

                // Console.WriteLine($"hrefPath: {hrefPath}");
                // Console.WriteLine($"woFragmentPath: {woFragmentPath}");

                // look if it is an internal link

                foreach (string contentLink in Program.ContentLinksList)
                {
                    if ("https://www.d20pfsrd.com" + woFragmentPath == contentLink)
                    {
                        isInternalLink = true;
                    }
                }

                if (!isInternalLink)
                {
                    // Console.WriteLine("not an internal link");
                    continue;
                }

                bool foundTOCItem = false;
                string TOCItem = "";

                // Console.WriteLine($"hasFragment: {hasFragment}");
                if (hasFragment)
                {
                    string linkFile = ConvertToMdTitle(woFragmentPath);
                    fragment = ConvertToMdTitle(href[href.IndexOf("#")..]);
                    fragment = fragment[1..];

                    // Console.WriteLine($"Fragment: {fragment}");

                    if (isSelfLink)
                    {
                        linkFile = title;
                    }

                    // Console.WriteLine($"linkFile: {linkFile}");

                    foreach (string heading in Headings[linkFile])
                    {
                        string h = heading.Replace("(", "");
                        h = h.Replace(")", "");
                        h = h.Trim();

                        string f = fragment.Replace("(", "");
                        f = f.Replace(")", "");
                        f = f.Trim();

                        // Console.WriteLine($" - {h}");

                        if (h == f)
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
                string subHtml = html[match.Index..];
                subHtml = Regex.Replace(subHtml, @"<img [^>]+?>", "");
                MatchCollection openingTags = Regex.Matches(subHtml, @"<[^(/|!)][^>]*?>");
                MatchCollection closingTags = Regex.Matches(subHtml, @"</[^(>|\-)]*?>");
                // Console.WriteLine(subHtml.Remove(400));
                // Console.WriteLine(openingTags.Count);
                // Console.WriteLine(closingTags.Count);

                bool isInHeading = false;
                int headingIndex = 0;
                int nextLineStartIndex = 0;
                int lineStartIndex = 0;

                for (int i = match.Index; i > 0; i--)
                {
                    if (html[i] == '\n')
                    {
                        lineStartIndex = i;
                        break;
                    }
                }

                for (int i = match.Index; i < html.Length; i++)
                {
                    if (html[i] == '\n')
                    {
                        nextLineStartIndex = i;
                        break;
                    }
                }

                if (html[lineStartIndex + 1] == '#')
                {
                    isInHeading = true;
                    headingIndex = lineStartIndex + 1;
                }

                string mdTitle = ConvertToMdTitle(hrefPath);

                // Console.WriteLine($"foundTOCItem: {foundTOCItem}");

                if (hasFragment && !foundTOCItem)
                {
                    mdTitle = RemoveFragment(mdTitle);
                    string d20pfsrdLink = href;

                    if (isSelfLink)
                    {
                        d20pfsrdLink = "https://www.d20pfsrd.com/" + title.Replace("_", "/") + href;
                    }

                    footer.Append($"[^{footerIndex}]: TOC-Item: {fragment} \nd20pfsrd-Link: {d20pfsrdLink}\n");
                }

                string linkText = "";
                // not in another element
                if (openingTags.Count == closingTags.Count)
                {
                    linkText = $"[[{mdTitle}|{content}]]";

                    if (hasFragment && !foundTOCItem)
                    {
                        if (isSelfLink)
                        {
                            linkText = $"[[{title}|{content}]]";
                        }
                        else
                        {
                            linkText = $"[[{ConvertToMdTitle(woFragmentPath)}|{content}]]";
                        }

                        linkText += $"[^{footerIndex}]";
                    }

                    // Console.WriteLine("replaced href with wikilink");
                }
                // in another element
                else if (openingTags.Count < closingTags.Count)
                {
                    if (!(hasFragment && !foundTOCItem))
                    {
                        linkText = $"<a data-href=\"{mdTitle}\" href=\"{mdTitle}\" class=\"internal-link\" target=\"_blank\" rel=\"noopener\">{content}</a>";
                    }
                    else
                    {
                        linkText = content;
                    }

                    linksAtEndOfFile.Append($"\n[[{mdTitle}|{content}]]");

                    /*
                    if (foundTOCItem)
                    {
                        linkText += $"[^{footerIndex}]";
                    }
                    */

                    // Console.WriteLine("replaced href a");
                }
                else
                {
                    linkText = $"[[{mdTitle}|{content}]]";

                    if (hasFragment && !foundTOCItem)
                    {
                        if (isSelfLink)
                        {
                            linkText = $"[[{title}|{content}]]";
                        }
                        else
                        {
                            linkText = $"[[{ConvertToMdTitle(woFragmentPath)}|{content}]]";
                        }

                        linkText += $"[^{footerIndex}]";
                    }

                    // Console.WriteLine("replaced href with wikilink");
                    Console.WriteLine("something is wrong");
                }

                if (isInHeading)
                {
                    // Console.WriteLine();
                    // Console.WriteLine(content);
                    // Console.WriteLine(linkText);
                    // Console.WriteLine(match.Value.Length);
                    // Console.WriteLine(nextLineStartIndex);

                    replacements.Add(new LinkReplacement(content, match.Value.Length, match.Index));

                    replacements.Add(new LinkReplacement($"\nLink from heading: {linkText}", 0, nextLineStartIndex));
                }
                else
                {
                    replacements.Add(new LinkReplacement(linkText, match.Value.Length, match.Index));
                }

                footerIndex += 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Encountered Exception: {e.Message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        LinkReplacement[] replacementsArr = replacements.ToArray();

        if (!IsSorted(replacementsArr))
        {
            replacementsArr = replacementsArr.OrderBy(x => x.Index).ToArray();
        }

        string[] splitHtml = SplitAt(html, replacementsArr.Select(x => x.Index).ToArray());

        // Console.WriteLine(replacementsArr.Length);
        // Console.WriteLine(splitHtml.Length);

        StringBuilder output = new StringBuilder();
        output.Append(splitHtml[0]);

        for (int i = 1; i < splitHtml.Length; i++)
        {
            // Console.WriteLine(splitHtml[i].Length + " - " + replacementsArr[i - 1].OriginalLength);
            // Console.WriteLine(splitHtml[i]);
            // Console.WriteLine(replacementsArr[i - 1].Index);
            // Console.WriteLine();

            // remove the old link
            string part = splitHtml[i][replacementsArr[i - 1].OriginalLength..];
            // add new one
            output.Append(replacementsArr[i - 1].Replacement).Append(part);
        }

        output.Append("\n\n").Append(linksAtEndOfFile);

        output.Append("\n\n").Append(footer);

        return output.ToString();
    }

    public string[] SplitAt(string source, params int[] index)
    {
        // index = index.OrderBy(x => x).ToArray();
        string[] output = new string[index.Length + 1];
        int pos = 0;

        for (int i = 0; i < index.Length; pos = index[i++])
        {
            output[i] = source[pos..index[i]];
        }

        output[index.Length] = source[pos..];
        return output;
    }

    public bool IsSorted(LinkReplacement[] arr)
    {
        int last = arr.Length - 1;
        if (last < 1)
        {
            return true;
        }

        int i = 0;

        while (i < last && arr[i].Index <= arr[i + 1].Index)
        {
            i++;
        }

        return i == last;
    }

    public struct LinkReplacement
    {
        public string Replacement { get; set; }
        public int OriginalLength { get; set; }
        public int Index { get; set; }

        public LinkReplacement(string replacement, int originalLength, int index)
        {
            Replacement = replacement;
            OriginalLength = originalLength;
            Index = index;
        }
    }
}