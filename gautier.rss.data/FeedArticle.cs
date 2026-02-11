using System.Diagnostics;

namespace gautier.rss.data;

public record FeedArticle(int DbId = -1, string FeedName = "", string HeadlineText = "", string ArticleSummary = "", string ArticleText = "", string ArticleDate = "", string ArticleUrl = "", string RowInsertDateTime = "");

