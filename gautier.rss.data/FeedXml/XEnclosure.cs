namespace gautier.rss.data.FeedXml
{
    /*
     * Class design credit goes to Newsboat RSS API
     * https://newsboat.org/
     * https://github.com/newsboat/newsboat/
     * https://www.newsbeuter.org/devel.html
     * https://github.com/akrennmair/newsbeuter
     */
    public class XEnclosure
    {
        public string Url { get; set; } = string.Empty;
        public string EnclosureType { get; set; } = string.Empty;
    }
}
