namespace gautier.rss.data.FeedXml
{
    /*
     * Class design credit goes to Newsboat RSS API
     * https://newsboat.org/
     * https://github.com/newsboat/newsboat/
     * https://www.newsbeuter.org/devel.html
     * https://github.com/akrennmair/newsbeuter
     */
    public class XFeed
    {
        public XDocType DocumentType { get; set; } = XDocType.Unknown;

        public string Encoding { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string TitleType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string ManagingEditor { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string PublicationDate { get; set; } = string.Empty;

        public List<XArticle> Articles { get; set; } = new();
        public string UpdatePeriod { get; set; } = string.Empty;
        public string UpdateFrequency { get; set; } = string.Empty;
        public string Generator { get; set; } = string.Empty;
    }
}
