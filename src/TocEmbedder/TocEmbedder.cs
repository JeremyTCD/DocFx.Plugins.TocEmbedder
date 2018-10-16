using HtmlAgilityPack;
using JeremyTCD.DocFx.Plugins.Utils;
using Microsoft.DocAsCode.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Text;

namespace JeremyTCD.DocFx.Plugins.OutlineGenerator
{
    [Export(nameof(TocEmbedder), typeof(IPostProcessor))]
    public class TocEmbedder : IPostProcessor
    {
        private static string[] _headingElementNames = new string[] { "h1", "h2", "h3", "h4", "h5", "h6" };

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            // Do nothing
            return metadata;
        }

        // If article menu is enabled, generates outline and inserts it
        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory cannot be null");
            }

            foreach (ManifestItem manifestItem in manifest.Files)
            {
                if (manifestItem.DocumentType != "Conceptual")
                {
                    continue;
                }

                // Category Menu
                manifestItem.Metadata.TryGetValue("mimo_disableCategoryMenu", out object disableCategoryMenu);
                if (disableCategoryMenu as bool? != true)
                {
                    // Get document path
                    string relPath = manifestItem.GetHtmlOutputRelPath();
                    Uri baseUri = new Uri(outputFolder + "\\");
                    Uri absUri = new Uri(baseUri, relPath);

                    // Get document Node
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.Load(absUri.AbsolutePath, Encoding.UTF8);
                    HtmlNode documentNode = htmlDoc.DocumentNode;

                    // Get TOC path
                    HtmlNode metaTocNode = documentNode.SelectSingleNode("//meta[@property='docfx:tocrel']");
                    string tocRelPath = metaTocNode.GetAttributeValue("content", null);
                    Uri tocAbsUri = new Uri(absUri, tocRelPath);

                    // Get TOC
                    HtmlDocument tocHtmlDoc = new HtmlDocument();
                    tocHtmlDoc.Load(tocAbsUri.AbsolutePath, Encoding.UTF8);
                    HtmlNode tocDocumentNode = tocHtmlDoc.DocumentNode;

                    // Insert SVGs
                    HtmlNode svgNode = tocHtmlDoc.CreateElement("svg");
                    HtmlNode useNode = tocHtmlDoc.CreateElement("use");
                    useNode.SetAttributeValue("xlink:href", "#material-design-chevron-right");
                    svgNode.AppendChild(useNode);
                    HtmlNodeCollection iconRequiringNodes = tocDocumentNode.SelectNodes("//li/a|//li/span");
                    foreach (HtmlNode htmlNode in iconRequiringNodes)
                    {
                        htmlNode.PrependChild(svgNode.Clone());
                    }

                    // Clean Hrefs (basically, make hrefs in toc absolute if toc is not in the same folder as the current document)
                    HtmlNodeCollection anchorNodes = tocDocumentNode.SelectNodes("//a");
                    string tocRelDir = Path.GetDirectoryName(tocRelPath);
                    if (tocRelDir != string.Empty)
                    {
                        foreach (HtmlNode anchorNode in anchorNodes)
                        {
                            string hrefRelToToc = anchorNode.GetAttributeValue("href", null);

                            // Don't do anything if its absolute
                            if (Uri.TryCreate(hrefRelToToc, UriKind.Absolute, out Uri uriRelToToc))
                            {
                                continue;
                            }

                            anchorNode.SetAttributeValue("href", tocRelDir + "/" + hrefRelToToc);
                        }
                    }

                    // Set active page 
                    string activeUri = absUri.AbsoluteUri.Replace(".html", "");
                    foreach (HtmlNode anchorNode in anchorNodes)
                    {
                        // TODO What to do if href is absolute?
                        Uri hrefUri = new Uri(anchorNode.GetAttributeValue("href", null), UriKind.Relative);
                        Uri pageAbsUri = new Uri(absUri, hrefUri);

                        if (pageAbsUri.AbsoluteUri == activeUri)
                        {
                            anchorNode.SetAttributeValue("class", "active");
                            break;
                        }
                    }

                    // Add TOC to page
                    HtmlNode categoryPagesNode = documentNode.SelectSingleNode("//*[@id='category-pages']");
                    categoryPagesNode.AppendChild(tocDocumentNode);
                    htmlDoc.Save(absUri.AbsolutePath);
                }

                // Navbar

                // TODO
                // Get TOC path
                // Get TOC
                // Set active category
                // Add navbar to page

                // Breadcrumbs

            }

            return manifest;
        }
    }
}
