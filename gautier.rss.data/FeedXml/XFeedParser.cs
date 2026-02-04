using System.Text;
using System.Xml;

namespace gautier.rss.data.FeedXml
{
    /*
     * Class design credit goes to Newsboat RSS API
     * https://newsboat.org/
     * https://github.com/newsboat/newsboat/
     * https://www.newsbeuter.org/devel.html
     * https://github.com/akrennmair/newsbeuter
     */
    public class XFeedParser
    {
        public const string XmlNamespaceContent = "http://purl.org/rss/1.0/modules/content/";
        public const string XmlNamespaceITunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        public const string XmlNamespaceDC = "http://purl.org/dc/elements/1.1/";
        public const string XmlNamespaceAtom0_3 = "http://purl.org/atom/ns#";
        public const string XmlNamespaceAtom1_0 = "http://www.w3.org/2005/Atom";
        public const string XmlNamespaceXml = "http://www.w3.org/XML/1998/namespace";

        private readonly XmlDocument _FeedDocument = new();
        private XDocType _DocType = XDocType.Unknown;

        private bool _ProcessingHeader = false;
        private bool _ProcessingEntry = false;

        // Future configurable entities
        private readonly Dictionary<string, string> _customEntities = new()
        {
            {
                "Uuml", "Ü"
            },// Add more entities as needed
        };

        public XDocType DocType
        {
            get => _DocType;
        }

        public XFeed ParseFile(string filePath)
        {
            XFeed feed = new();
            // Generate and insert the custom DTD properly
            string dtd = GenerateCustomDTD();
            string xmlContent = File.ReadAllText(filePath);
            // Place the DTD directly after the XML declaration
            int declarationEnd = xmlContent.IndexOf("?>") + 2;
            xmlContent = xmlContent.Insert(declarationEnd, dtd);
            // Load the XML file using XmlDocument with the custom DTD
            _FeedDocument.XmlResolver = null;// Prevent external DTD fetching
            _FeedDocument.LoadXml(xmlContent);
            XmlElement? rootNode = _FeedDocument.DocumentElement ?? _FeedDocument.CreateElement("rss");
            ParseNode(rootNode, feed);
            return feed;
        }

        private string GenerateCustomDTD()
        {
            StringBuilder? dtdBuilder = new();
            dtdBuilder.AppendLine("<!DOCTYPE rss [");

            foreach (KeyValuePair<string, string> entity in _customEntities)
            {
                dtdBuilder.AppendLine($"<!ENTITY {entity.Key} \"{entity.Value}\">");
            }

            dtdBuilder.AppendLine("]>");
            return dtdBuilder.ToString();
        }

        private void ParseNode(XmlElement node, XFeed feed)
        {
            XArticle Article = feed.Articles.Count > 0 ? feed.Articles.Last() : new();

            switch (node.Name)
            {
                /*Feed/Atom Level*/
                case "feed":
                    {
                        if (_ProcessingEntry == false)
                        {
                            _ProcessingHeader = true;
                            _DocType = XDocType.ATOM_1_0;
                            DetectATOMVersion(node);
                        }
                    }
                    break;

                case "rss":
                    {
                        if (_ProcessingEntry == false)
                        {
                            _ProcessingHeader = true;
                            DetectRSSVersion(node);
                        }
                    }
                    break;

                case "RDF":
                    {
                        if (_ProcessingEntry == false)
                        {
                            _ProcessingHeader = true;
                            _DocType = XDocType.RDF;
                        }
                    }
                    break;

                case "channel":
                    {
                        if (_ProcessingEntry == false)
                        {
                            _ProcessingHeader = true;
                        }
                    }
                    break;

                case "title":
                    {
                        if (_ProcessingEntry)
                        {
                            Article.Title = node.InnerText;
                        }

                        else if (_ProcessingHeader)
                        {
                            feed.Title = node.InnerText;
                        }
                    }
                    break;

                case "link":
                case "id":
                    {
                        if (_ProcessingEntry)
                        {
                            string HRef = node.InnerText;

                            if (string.IsNullOrWhiteSpace(HRef))
                            {
                                HRef = GetHrefAttrValue(node);
                            }

                            Article.Link = HRef;
                        }

                        else if (_ProcessingHeader)
                        {
                            feed.Link = node.InnerText;
                        }
                    }
                    break;

                case "description":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.Description = node.InnerText;
                        }

                        else if (_ProcessingEntry)
                        {
                            Article.Description = node.InnerText;
                        }
                    }
                    break;

                case "lastBuildDate":
                case "pubDate":
                case "date":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.PublicationDate = node.InnerText;
                        }

                        else if (_ProcessingEntry)
                        {
                            Article.PublicationDate = node.InnerText;
                        }
                    }
                    break;

                case "language":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.Language = node.InnerText;
                        }
                    }
                    break;

                case "updatePeriod":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.UpdatePeriod = node.InnerText;
                        }
                    }
                    break;

                case "updateFrequency":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.UpdateFrequency = node.InnerText;
                        }
                    }
                    break;

                case "generator":
                    {
                        if (_ProcessingHeader)
                        {
                            feed.Generator = node.InnerText;
                        }
                    }
                    break;

                case "item":
                case "entry":
                    {
                        if (_ProcessingHeader)
                        {
                            _ProcessingHeader = false;
                            _ProcessingEntry = true;
                        }

                        Article = new();
                        feed.Articles.Add(Article);
                    }
                    break;

                case "creator":
                    {
                        if (_ProcessingEntry)
                        {
                            Article.Creator = node.InnerText;
                        }
                    }
                    break;

                case "guid":
                    {
                        if (_ProcessingEntry)
                        {
                            Article.Guid = node.InnerText;

                            if (string.IsNullOrWhiteSpace(Article.Link))
                            {
                                Article.Link = node.InnerText;
                            }

                            foreach (XmlAttribute Attr in node.Attributes)
                            {
                                switch (Attr.Name)
                                {
                                    case "isPermaLink":
                                        {
                                            Article.GuidIsPermaLink = node.Value == "true";
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    break;

                case "encoded":
                case "summary":
                    {
                        if (_ProcessingEntry)
                        {
                            Article.ContentEncoded = node.InnerText;
                        }
                    }
                    break;
            }

            foreach (XmlNode NextElement in node.ChildNodes)
            {
                if (NextElement.NodeType == XmlNodeType.Element)
                {
                    XmlElement E = NextElement as XmlElement ?? new XmlDocument().CreateElement("empty");
                    ParseNode(E, feed);
                }
            }

            return;
        }

        private static string GetHrefAttrValue(XmlElement node)
        {
            string HRef = string.Empty;

            foreach (XmlAttribute Attr in node.Attributes)
            {
                switch (Attr.Name)
                {
                    case "href":
                        {
                            HRef = Attr.Value;
                        }
                        break;
                }

                if (string.IsNullOrWhiteSpace(HRef) == false)
                {
                    break;
                }
            }

            return HRef;
        }

        private void DetectRSSVersion(XmlElement node)
        {
            foreach (XmlAttribute Attr in node.Attributes)
            {
                switch (Attr.Name)
                {
                    case "version":
                        {
                            switch (Attr.Value)
                            {
                                case "0.91":
                                    {
                                        _DocType = XDocType.RSS_0_91;
                                    }
                                    break;

                                case "0.92":
                                    {
                                        _DocType = XDocType.RSS_0_92;
                                    }
                                    break;

                                case "1.0":
                                    {
                                        _DocType = XDocType.RSS_1_0;
                                    }
                                    break;

                                case "2.0":
                                    {
                                        _DocType = XDocType.RSS_2_0;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            return;
        }

        private void DetectATOMVersion(XmlElement node)
        {
            string NameSpaceValue = node.NamespaceURI;

            switch (NameSpaceValue)
            {
                case XmlNamespaceAtom0_3:
                    {
                        _DocType = XDocType.ATOM_0_3_NONS;
                    }
                    break;

                case XmlNamespaceAtom1_0:
                    {
                        _DocType = XDocType.ATOM_1_0;
                    }
                    break;

                default:
                    {
                        CheckAtomAttributes(node);
                    }
                    break;
            }

            return;
        }

        private void CheckAtomAttributes(XmlElement node)
        {
            foreach (XmlAttribute Attr in node.Attributes)
            {
                switch (Attr.Name)
                {
                    case "version":
                        {
                            switch (Attr.Value)
                            {
                                case "0.3":
                                    {
                                        _DocType = XDocType.ATOM_0_3;
                                    }
                                    break;

                                case "1.0":
                                    {
                                        _DocType = XDocType.ATOM_1_0;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }

            return;
        }
    }
}
