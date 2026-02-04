namespace gautier.rss.data.FeedXml
{
    /*
     * Class design credit goes to Newsboat RSS API
     * https://newsboat.org/
     * https://github.com/newsboat/newsboat/
     * https://www.newsbeuter.org/devel.html
     * https://github.com/akrennmair/newsbeuter
     */
    public class XArticle
    {
        public string Title { get; set; } = string.Empty;
        public string TitleType { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionMimeType { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;

        public string PublicationDate { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public bool GuidIsPermaLink { get; set; } = false;

        public List<XEnclosure> Enclosures { get; set; } = new();

        public string ContentEncoded { get; set; } = string.Empty;
        public string iTunesSummary { get; set; } = string.Empty;

        public List<string> Labels { get; set; } = new();

        public DateTime PublicationDateTimeStamp { get; set; } = DateTime.UtcNow;
        public string Creator { get; set; } = string.Empty;
    }
}
