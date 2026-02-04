namespace gautier.rss.data
{
    public class FeedArticleUnion
    {
        public Feed FeedHeader { get; set; } = new();
        public FeedArticle ArticleDetail { get; set; } = new();
    }
}
