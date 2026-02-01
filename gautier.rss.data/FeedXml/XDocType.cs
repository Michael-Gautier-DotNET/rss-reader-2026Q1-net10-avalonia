namespace gautier.rss.data.FeedXml
{
    /*
     * Enum design credit goes to Newsboat RSS API
     * https://newsboat.org/
     * https://github.com/newsboat/newsboat/
     * https://www.newsbeuter.org/devel.html
     * https://github.com/akrennmair/newsbeuter
     */
    public enum XDocType
    {
        Unknown = 0,
        RSS_0_91,
        RSS_0_92,
        RSS_1_0,
        RSS_2_0,
        ATOM_0_3,
        ATOM_1_0,
        RSS_0_94,
        ATOM_0_3_NONS,
        RDF,
    }
}
