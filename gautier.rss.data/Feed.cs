using System.Diagnostics;

namespace gautier.rss.data;

public record Feed(int DbId = -1, string FeedName = "", string FeedUrl = "", string LastRetrieved = "", string RetrieveLimitHrs = "", string RetentionDays = "", string RowInsertDateTime = "");

