
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace d20pfsrd_web_scraper
{
    public class MdConverter
    {
        public MdConverter()
        {
            
        }

        public void LoadAndConvert(string path, string relPath, string outPath)
        {
            Console.WriteLine(outPath);
            string html = File.ReadAllText(path + "index.html", System.Text.Encoding.UTF8);
            ConvertToMd(html, path, relPath, outPath);
        }

        public void ConvertToMd(string html, string path, string relPath, string outPath)
        {
            string title = ConvertToMdTitle(relPath);

            string markdown = @"# " + GetDocumentHeadingFromMdTitle(title) + "\n";

            markdown += ParseHtml(html);

            // Directory.CreateDirectory(outPath);

            File.WriteAllText(outPath + title + ".md", markdown, System.Text.Encoding.UTF8);
        }

        public string ParseHtml(string html)
        {            
            html = html.Trim();

            html = Regex.Replace(html, @"<!--.*?-->", "");

            html = Regex.Replace(html, @"<p[^>]*>", "\n");
            html = Regex.Replace(html, @"</p[^>]*>", "");

            html = Regex.Replace(html, @"</?span[^>]*>", "");

            html = Regex.Replace(html, @"</?div[^>]*>", "");

            html = Regex.Replace(html, @"<h1[^>]*>", "\n# ");
            html = Regex.Replace(html, @"</h1[^>]*>", "\n");

            html = Regex.Replace(html, @"<h2[^>]*>", "\n## ");
            html = Regex.Replace(html, @"</h2[^>]*>", "\n");

            html = Regex.Replace(html, @"<h3[^>]*>", "\n### ");
            html = Regex.Replace(html, @"</h3[^>]*>", "\n");

            html = Regex.Replace(html, @"<h4[^>]*>", "\n#### ");
            html = Regex.Replace(html, @"</h4[^>]*>", "\n");

            html = Regex.Replace(html, @"<h5[^>]*>", "\n##### ");
            html = Regex.Replace(html, @"</h5[^>]*>", "\n");

            html = Regex.Replace(html, @"<h6[^>]*>", "\n###### ");
            html = Regex.Replace(html, @"</h6[^>]*>", "\n");


            html = Regex.Replace(html, @" *<i> *", " *");
            html = Regex.Replace(html, @" *</i> *", "* ");

            html = Regex.Replace(html, @" *<b> *", " **");
            html = Regex.Replace(html, @" *</b> *", "** ");

            html = Regex.Replace(html, @"</?ul[^>]*>", "");

            html = Regex.Replace(html, @"<li[^>]*>", " - ");
            html = Regex.Replace(html, @"</li>", "\n");

            html = Regex.Replace(html, @"<a (name|id)[^>]*></a>", "");

            html = Regex.Replace(html, @"<br>", "\n");

            html = ConvertLinks(html);


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
            string[] parts = mdTitle.Split('_');
            return FirstCharToUpper(parts[parts.Length - 1]);
        }

        public string FirstCharToUpper(string input)
        {
            return input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input[0].ToString().ToUpper() + input.Substring(1)
            };
        }

        public string RemoveTrailingSlashes(string link)
        {
            if (link.EndsWith('/') || link.EndsWith('\\'))
            {
                return link.Remove(link.Length - 1);
            }

            return link;
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

        public string ConvertLinks(string html)
        {
            // get all a tags
            MatchCollection links = Regex.Matches(html, @"<a[^>]*>.*?</a>");

            List<string> replacementList = new List<string>();
            List<int> origionalLength = new List<int>();
            List<int> indices = new List<int>();

            string linksAtEndOfFile = "";

            foreach (Match match in links)
            {
                // get the href
                Match hrefMatch = Regex.Match(match.Value, " href=\".+?\"");
                if(!hrefMatch.Success)
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

                try
                {
                    string hrefPath = "";
                    string woFragmentPath = "";
                    bool isSelfLink = false;

                    if (href.StartsWith('#'))
                    {
                        hrefPath = href;
                        woFragmentPath = href;
                        isSelfLink = true;
                    }
                    else
                    {
                        Uri hrefUri = new Uri(href);
                        hrefPath = hrefUri.AbsolutePath + hrefUri.Fragment;
                        woFragmentPath = RemoveFragment(href);
                    }
                    // Console.WriteLine(href);
                    // Console.WriteLine(hrefPath);

                    // Console.WriteLine(hrefPath);
                    bool reformat = false;
                    if (!isSelfLink)
                    {
                        foreach (string contentLink in Program.ContentLinksList)
                        {
                            // TODO: maybe do href == contentlink to fix broken links
                            if (RemoveTrailingSlashes(woFragmentPath) == RemoveTrailingSlashes(contentLink))
                            {
                                reformat = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        reformat = true;
                    }
                    if (reformat)
                    {
                        string subHtml = html.Substring(match.Index);
                        subHtml = Regex.Replace(subHtml, @"<img [^>]+?>", "");
                        MatchCollection openingTags = Regex.Matches(subHtml, @"<[^(/|!)][^>]*?>");
                        MatchCollection closingTags = Regex.Matches(subHtml, @"</[^(>|\-)]*?>");

                        bool isInHeading = false;
                        int headingIndex = 0;
                        int nextLineStartIndex = 0;
                        int lineStartIndex = 0;

                        for(int i = match.Index; i > 0; i--)
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


                        // not in another element
                        if (openingTags.Count == closingTags.Count)
                        {
                            string mdTitle = "";
                            if (isSelfLink)
                            {
                                mdTitle = ConvertToMdTitle(hrefPath);
                            }
                            else
                            {
                                mdTitle = ConvertToMdTitle(hrefPath);
                            }
                            if (mdTitle == "")
                            {
                                continue;
                            }

                            if (isInHeading)
                            {
                                string replacement = $"{content}";
                                replacementList.Add(replacement);
                                origionalLength.Add(match.Value.Length);
                                indices.Add(match.Index);

                                replacementList.Add($"\nLink from heading: [[{mdTitle}|{content}]]");
                                origionalLength.Add(0);
                                indices.Add(nextLineStartIndex);// - match.Value.Length + replacement.Length);
                            }
                            else
                            {
                                replacementList.Add($"[[{mdTitle}|{content}]]");
                                origionalLength.Add(match.Value.Length);
                                indices.Add(match.Index);
                            }

                            // Console.WriteLine("replaced href with wikilink");
                            
                        }
                        // in another element
                        else if (openingTags.Count < closingTags.Count)
                        {
                            string mdTitle = "";
                            if (isSelfLink)
                            {
                                mdTitle = ConvertToMdTitle(hrefPath);
                            }
                            else
                            {
                                mdTitle = ConvertToMdTitle(hrefPath);
                            }
                            if (mdTitle == "")
                            {
                                continue;
                            }

                            if (isInHeading)
                            {
                                string replacement = $"{content}";
                                replacementList.Add(replacement);
                                origionalLength.Add(match.Value.Length);
                                indices.Add(match.Index);

                                replacementList.Add($"\nLink from heading: <a data-href=\"{mdTitle}\" href=\"{mdTitle}\" class=\"internal-link\" target=\"_blank\" rel=\"noopener\">{content}</a>");
                                origionalLength.Add(0);
                                indices.Add(nextLineStartIndex);// - match.Value.Length + replacement.Length);
                            }
                            else
                            {
                                replacementList.Add($"<a data-href=\"{mdTitle}\" href=\"{mdTitle}\" class=\"internal-link\" target=\"_blank\" rel=\"noopener\">{content}</a>");
                                origionalLength.Add(match.Value.Length);
                                indices.Add(match.Index);
                            }

                            // Console.WriteLine("replaced href a");
                            linksAtEndOfFile += "\n" + $"[[{mdTitle}|{content}]]";
                        }
                        else
                        {
                            // Console.WriteLine("Something is wrong");
                        }
                    }
                    else
                    {
                        // Console.WriteLine("cant find href");
                    }
                }
                catch (UriFormatException)
                {
                    // Console.WriteLine("cant read href");
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
                // Console.WriteLine(splitHtml[i]);
                // Console.WriteLine(origionalLength[i - 1]);
                // remove the old link
                string part = splitHtml[i].Substring(origionalLength[i - 1]);
                // add new one
                output += replacementList[i - 1] + part;
            }

            output += "\n\n\n# Obsidian Links for HTML links in file\n" + linksAtEndOfFile;

            return output;
        }

        public string[] SplitAt(string source, params int[] index)
        {
            index = index.Distinct().OrderBy(x => x).ToArray();
            string[] output = new string[index.Length + 1];
            int pos = 0;

            for (int i = 0; i < index.Length; pos = index[i++])
                output[i] = source[pos..index[i]];

            output[index.Length] = source[pos..];
            return output;
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
}