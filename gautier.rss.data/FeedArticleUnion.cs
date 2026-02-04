namespace gautier.rss.data
{
    public struct FeedArticleUnion
    {
        public FeedArticleUnion() { }
        public Feed FeedHeader { get; set; } = new();
        public FeedArticle ArticleDetail { get; set; } = new();
    }
}
