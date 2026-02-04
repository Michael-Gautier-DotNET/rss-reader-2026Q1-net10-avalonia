using System.Diagnostics;

namespace gautier.rss.data
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class FeedArticle
    {
        public int DbId { get; set; } = -1;
        public string FeedName { get; set; } = string.Empty;
        public string HeadlineText { get; set; } = string.Empty;
        public string ArticleSummary { get; set; } = string.Empty;
        public string ArticleText { get; set; } = string.Empty;
        public string ArticleDate { get; set; } = string.Empty;
        public string ArticleUrl { get; set; } = string.Empty;
        public string RowInsertDateTime { get; set; } = string.Empty;

        private string GetDebuggerDisplay() => $"{FeedName} {HeadlineText} {ArticleUrl}";
    }
}
