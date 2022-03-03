using System.Text;
using System.Text.RegularExpressions;

namespace d20pfsrd_web_scraper;

public class MdConverter
{
    public static Dictionary<string, List<string>> Headings;

    public static void Init()
    {
        Headings = new Dictionary<string, List<string>>(Program.ContentLinksList.Length);
        foreach (string contentLink in Program.ContentLinksList)
        {
            Uri hrefUri = new Uri(contentLink);
            string hrefPath = hrefUri.AbsolutePath;

            Headings[ConvertToMdTitle(hrefPath)] = new List<string>();
        }
    }

    public static string LoadAndConvert(NoteMetadata noteMetadata)
    {
        // Console.WriteLine(PathHelper.Combine(Program.RunLocation, noteMetadata.LocalPathToHtml));

        string html = File.ReadAllText(PathHelper.Combine(Program.RunLocation, noteMetadata.LocalPathToHtml), Encoding.UTF8);
        return ConvertToMd(html, noteMetadata);
    }

    public static string ConvertToMd(string html, NoteMetadata noteMetadata)
    {
        // Console.WriteLine(PathHelper.Combine(Program.RunLocation, note.LocalPathToMarkdown));

        string markdown = noteMetadata.ToMetadata() + "\n";
        markdown += "# " + noteMetadata.Title + "\n";
        markdown += ParseHtml(html, noteMetadata.FileName);

        // Directory.CreateDirectory(PathHelper.Combine(Program.RunLocation, Program.OutputFolder, noteMetadata.LocalPathToFolder));

        return markdown;
    }

    public static string ConvertLinks(NoteMetadata noteMetadata)
    {
        // Console.WriteLine(PathHelper.Combine(Program.RunLocation, noteMetadata.LocalPathToMarkdown));

        string document = File.ReadAllText(PathHelper.Combine(Program.RunLocation, noteMetadata.LocalPathToMarkdown), Encoding.UTF8);

        document = UpdateLinks(document, noteMetadata.FileName);

        return document;
    }

    public static string ParseHtml(string html, string title)
    {
        html = html.Trim();

        // replace new line stuff
        html = html.Replace("\r\n", "\n");
        html = html.Replace("\r", "\n");

        // remove html comments
        html = Regex.Replace(html, @"<!--.*?>", "");

        // remove p tags
        html = Regex.Replace(html, @"</?p[^>]*>", "\n");

        // remove span tags
        html = Regex.Replace(html, @"</?span[^>]*>", "");

        // replace FAQs with block quote placeholders
        MatchCollection faqs = Regex.Matches(html, "<div class=\"faq.*?\">");
        html = AddQuotePlaceholders(html, faqs);
        
        // replace content sidebars with block quote placeholders
        MatchCollection contentSidebars = Regex.Matches(html, "<div class=\"content-sidebar\">");
        html = AddQuotePlaceholders(html, contentSidebars);

        // replace divs
        html = Regex.Replace(html, @"</?div[^>]*>", "");

        // replace headings
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

        // replace itallic tags
        html = Regex.Replace(html, @" *<i> *", " *");
        html = Regex.Replace(html, @" *</i> *", "* ");

        // replace bold tags
        html = Regex.Replace(html, @" *<b> *", " **");
        html = Regex.Replace(html, @" *</b> *", "** ");
        html = Regex.Replace(html, @" *<strong> *", " **");
        html = Regex.Replace(html, @" *</strong> *", "** ");
        
        // replace lists
        html = Regex.Replace(html, @"</?ul[^>]*>", "\n");
        html = Regex.Replace(html, @"<li[^>]*>", " - ");
        html = Regex.Replace(html, @"</li>", "\n");
        html = Regex.Replace(html, @" -  +", " - ");

        // remove TOC anchors
        html = Regex.Replace(html, @"<a (name|id)[^>]*></a>", "");

        // replace hr and br
        html = Regex.Replace(html, @"<br>", "\n");
        html = Regex.Replace(html, @"<hr>", "\n---\n");

        html = Regex.Replace(html, @"\* :", "*:");

        // remove all sort of empty line problems
        html = html.Replace("\r", "\n");
        
        html = Regex.Replace(html, "\n\n\n+", "\n\n");

        // html = Regex.Replace(html, "\n#", "\n\n#");
        html = Regex.Replace(html, "\n +?<", "\n<");
        html = Regex.Replace(html, "\n\n<", "\n<");
        html = Regex.Replace(html, "\n\n - ", "\n - ");

        // replace weired special characters with their normal counterparts
        html = Regex.Replace(html, "&amp;", "&"); // & -> &
        html = Regex.Replace(html, "&quot;", "\""); // " -> "
        html = Regex.Replace(html, "&apos;", "\'"); // ' -> '

        html = Regex.Replace(html, "&#034;", "\""); // " -> "
        html = Regex.Replace(html, "&#039;", "\'"); // ' -> '
        
        html = Regex.Replace(html, "&#8216;", "\'"); // ‘ -> '
        html = Regex.Replace(html, "&#8217;", "\'"); // ’ -> '
        html = Regex.Replace(html, "&#8218;", "\'"); // ‚ -> '
        
        html = Regex.Replace(html, "&#8220;", "\""); // “ -> "
        html = Regex.Replace(html, "&#8221;", "\""); // ” -> "
        html = Regex.Replace(html, "&#8222;", "\""); // „ -> "
        
        html = Regex.Replace(html, "&#8211;", "--"); // – -> --
        html = Regex.Replace(html, "&#8212;", "--"); // — -> --

        // resolve all quote placeholders
        html = ResolveQuotePlaceholders(html);

        // add all the headings into the heading list
        MatchCollection headings = Regex.Matches(html, "#+? .+?\n");
        foreach (Match heading in headings)
        {
            string v = heading.Value;
            // remove the hashtags from the beginning
            v = Regex.Replace(v, "#+? ", "");
            // remove the new line from the end
            v = Regex.Replace(v, "\n", "");
            v = v.Trim();
            // remove any links
            v = Regex.Replace(v, @"<a[^>]*>", "");
            v = Regex.Replace(v, @"</a>", "");
            
            Headings[title].Add(v);
        }

        return html;
    }

    public static string ConvertToMdTitle(string relPath)
    {
        // replace slashes with underscores
        string title = relPath.Replace('/', '_');
        title = title.Replace('\\', '_');

        // replace fragment
        int hashtagIndex = title.IndexOf('#');
        string subTitle = "";
        if (hashtagIndex != -1)
        {
            subTitle = title.Substring(hashtagIndex, title.Length - hashtagIndex);
            subTitle = subTitle.Replace("TOC-", "");
            subTitle = subTitle.Replace("toc-", "");
            if (int.TryParse(subTitle[1..], out _))
            {
                subTitle = "";
            }

            subTitle = subTitle.Replace('-', ' ');
            subTitle = subTitle.Replace('_', ' ');
            title = title.Remove(hashtagIndex);
        }

        // trim trailing underscores
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

    public static string GetDocumentHeadingFromMdTitle(string mdTitle)
    {
        if (mdTitle == "")
        {
            mdTitle = "index";
        }

        string[] parts = mdTitle.Split('_');
        return FirstCharToUpper(parts[parts.Length - 1]);
    }

    public static string FirstCharToUpper(string input)
    {
        return input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1)),
        };
    }

    public static string RemoveFragment(string path)
    {
        int hashtagIndex = path.IndexOf('#');
        return hashtagIndex == -1 ? path : path.Remove(hashtagIndex);
    }

    public static string UpdateLinks(string html, string title)
    {
        // so that we only complain about the html once
        bool complainedAboutHtml = false;
        
        // get all a tags
        MatchCollection links = Regex.Matches(html, @"<a[^>]*>.*?</a>");

        List<Replacement> replacements = new List<Replacement>();

        StringBuilder linksAtEndOfFile = new StringBuilder();
        StringBuilder footer = new StringBuilder();
        
        // a index for generating the footer number
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

            // if does not link to this site it is irrelevant
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
                int hrefHashtagIndex = -1;

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

                hrefHashtagIndex = href.IndexOf('#');
                if (hrefHashtagIndex != -1)
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
                if (!isInternalLink)
                {
                    string d20LinkWoFragment = "https://www.d20pfsrd.com" + woFragmentPath;
                    if (Program.ContentLinksList.Contains(d20LinkWoFragment))
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
                    fragment = ConvertToMdTitle(href[hrefHashtagIndex..]);
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
                subHtml = Regex.Replace(subHtml, @"<area[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<base[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<br[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<col[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<command[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<embed[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<hr[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<img[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<input[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<keygen[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<link[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<meta[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<param[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<source[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<track[^>]*?>", "");
                subHtml = Regex.Replace(subHtml, @"<wbr[^>]*?>", "");
                MatchCollection openingTags = Regex.Matches(subHtml, @"<[^/][^>]*?>");
                MatchCollection closingTags = Regex.Matches(subHtml, @"<\/[^>]*?>");
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
                    if (!complainedAboutHtml)
                    {
                        Console.WriteLine("Something in the source HTML is wrong");
                        complainedAboutHtml = true;
                    }
                }

                if (isInHeading)
                {
                    // Console.WriteLine();
                    // Console.WriteLine(content);
                    // Console.WriteLine(linkText);
                    // Console.WriteLine(match.Value.Length);
                    // Console.WriteLine(nextLineStartIndex);

                    replacements.Add(new Replacement(content, match.Value.Length, match.Index));

                    replacements.Add(new Replacement($"\nLink from heading: {linkText}", 0, nextLineStartIndex));
                }
                else
                {
                    replacements.Add(new Replacement(linkText, match.Value.Length, match.Index));
                }

                footerIndex += 1;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Encountered Exception: {e.Message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        Replacement[] replacementsArr = replacements.ToArray();

        StringBuilder output = Replace(html, replacementsArr);

        output.Append("\n\n").Append(linksAtEndOfFile);

        output.Append("\n\n").Append(footer);

        return output.ToString();

    }

    public static string AddQuotePlaceholders(string html, MatchCollection matchCollection)
    {
        List<Replacement> replacements = new List<Replacement>();

        foreach (Match match in matchCollection)
        {
            string subHtml = html[match.Index..];
            MatchCollection openingTags = Regex.Matches(subHtml, @"<[^/]iv[^>]*?>");
            MatchCollection closingTags = Regex.Matches(subHtml, @"<\/div[^>]*?>");
            
            // Console.WriteLine($"Opening tags count: {openingTags.Count}");
            // Console.WriteLine($"Closing tags count: {closingTags.Count}");

            Match? quoteEndTag = null;
            int depth = 0;
            bool foundFirst = false;
            for (int i = 0; i < subHtml.Length; i++)
            {
                foreach (Match openingTag in openingTags)
                {
                    if (openingTag.Index > i)
                    {
                        break;
                    }

                    if (openingTag.Index == i)
                    {
                        depth += 1;
                        foundFirst = true;
                    }
                }
                
                foreach (Match closingTag in closingTags)
                {
                    if (closingTag.Index > i)
                    {
                        break;
                    }

                    if (closingTag.Index == i)
                    {
                        depth -= 1;
                    }
                }

                if (foundFirst && depth == 0)
                {
                    // Console.WriteLine(i);
                    // Console.WriteLine(depth);
                    quoteEndTag = closingTags.First(t => t.Index == i);
                    break;
                }
            }

            if (quoteEndTag == null)
            {
                continue;
            }
            
            replacements.Add(new Replacement("---quoteStart---", match.Length, match.Index));
            replacements.Add(new Replacement("---quoteEnd---", quoteEndTag.Length, match.Index + quoteEndTag.Index));
        }
        
        Replacement[] replacementsArr = replacements.ToArray();

        StringBuilder output = Replace(html, replacementsArr);

        return output.ToString();
    }

    public static string ResolveQuotePlaceholders(string html)
    {
        Match[] quoteStarts = Regex.Matches(html, @"---quoteStart---").ToArray();
        Match[] quoteEnds = Regex.Matches(html, @"---quoteEnd---").ToArray();

        if (quoteStarts.Length != quoteEnds.Length)
        {
            Console.WriteLine("Quote Starts do not match Quote Ends");
            return html;
        }
        
        List<Replacement> replacements = new List<Replacement>();

        for (int i = 0; i < quoteStarts.Length; i++)
        {
            string subHtml = html[quoteStarts[i].Index..(quoteEnds[i].Index + quoteEnds[i].Value.Length)];
            int orgLength = subHtml.Length;

            subHtml = subHtml.Replace(@"---quoteStart---", "");
            subHtml = subHtml.Replace(@"---quoteEnd---", "");
            subHtml = Regex.Replace(subHtml, "\n\n+", "\n");
            subHtml = Regex.Replace(subHtml, "\n", "\n> ");
            subHtml = Regex.Replace(subHtml, ">  +", "> ");
            
            if (subHtml.EndsWith("\n> "))
            {
                subHtml = subHtml[..^3];
            }

            replacements.Add(new Replacement(subHtml, orgLength, quoteStarts[i].Index));
        }
        
        Replacement[] replacementsArr = replacements.ToArray();

        StringBuilder output = Replace(html, replacementsArr);

        return output.ToString();
    }

    public static StringBuilder Replace(string str, Replacement[] replacements)
    {
        if (!IsSorted(replacements))
        {
            replacements = replacements.OrderBy(x => x.Index).ToArray();
        }

        string[] splitHtml = SplitAt(str, replacements.Select(x => x.Index).ToArray());

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
            string part = splitHtml[i][replacements[i - 1].OriginalLength..];
            // add new one
            output.Append(replacements[i - 1].Value).Append(part);
        }

        return output;
    }
    

    public static string[] SplitAt(string source, params int[] index)
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

    public static bool IsSorted(Replacement[] arr)
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

    public struct Replacement
    {
        public string Value { get; set; }
        public int OriginalLength { get; set; }
        public int Index { get; set; }

        public Replacement(string value, int originalLength, int index)
        {
            Value = value;
            OriginalLength = originalLength;
            Index = index;
        }
    }
}