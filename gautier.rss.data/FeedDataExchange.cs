using System.Data.SQLite;
using System.Globalization;

using gautier.rss.data.RSSDb;

namespace gautier.rss.data
{
    public static class FeedDataExchange
    {
        private const char _Tab = '\t';

        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;

        public static List<Feed> GetAllFeeds(string sqlConnectionString)
        {
            List<Feed> Feeds = new();

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                List<Feed> FeedEntries = FeedReader.GetAllRows(SQLConn);

                if (FeedEntries.Any())
                {
                    Feeds = FeedEntries;
                }
            }

            return Feeds;
        }

        public static int GetMaxArticleID(string sqlConnectionString, string feedName)
        {
            int Id = -1;

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                Id = FeedArticleReader.GetMaxId(SQLConn, feedName);
            }

            return Id;
        }

        public static List<FeedArticle> GetFeedArticles(string sqlConnectionString, string feedName, int idBegin, int idEnd)
        {
            List<FeedArticle> Articles = new(100);

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                Articles = FeedArticleReader.GetRows(SQLConn, feedName, idBegin, idEnd);
            }

            return Articles;
        }

        public static void DownloadFeed(Feed feed, string downloadDir, string databasePath)
        {
            string RSSXmlFilePath = Path.Combine(downloadDir, $"{feed.FeedName}.xml");

            bool FeedDownloaded = RSSNetClient.DownloadFeed(RSSXmlFilePath, feed);

            /*Leave these quick diagnostic statements. They are useful in a pinch.*/
            //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} {RSSXmlFilePath}");

            if (FeedDownloaded && File.Exists(RSSXmlFilePath))
            {
                string RSSIntegrationFilePath =
                    FeedFileUtil.GetRSSTabDelimitedFeedFilePath(downloadDir, feed);

                //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} {RSSIntegrationFilePath}");

                List<FeedArticle> Articles =
                    FeedFileConverter.TransformXmlFeedToFeedArticles(downloadDir,
                        feed);

                //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} saved to: {downloadDir}");

                string RSSTabDelimitedFilePath =
                    FeedFileConverter.WriteRSSArticlesToFile(downloadDir, feed,
                        Articles);

                //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} delimited in: {downloadDir}");

                bool RSSIntegrationPathIsValid = RSSIntegrationFilePath == RSSTabDelimitedFilePath;

                //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} integration path valid: {RSSIntegrationPathIsValid}");
                if (RSSIntegrationPathIsValid && File.Exists(RSSTabDelimitedFilePath))
                {
                    //Console.WriteLine($"\t\t UI {nameof(DownloadFeed)} {feed.FeedName} {feed.FeedUrl} DATABASE IMPORT {RSSTabDelimitedFilePath}");
                    FeedDataExchange.ImportRSSFeedToDatabase(downloadDir, databasePath, feed);
                }
            }
        }

        public static void ImportStaticFeedFilesToDatabase(string feedSaveDirectoryPath, string feedDbFilePath, Feed[] feeds)
        {
            foreach (Feed FeedEntry in feeds)
            {
                ImportRSSFeedToDatabase(feedSaveDirectoryPath, feedDbFilePath, FeedEntry);
            }

            //Console.WriteLine($"\t\t/////////////////Updated SQLite database | {feedDbFilePath}");
            return;
        }

        public static void ImportRSSFeedToDatabase(string feedSaveDirectoryPath, string feedDbFilePath, Feed feed)
        {
            List<FeedArticle> Articles = new();
            DateTime ModificationDateTime = DateTime.Now;
            string ModificationDateTimeText = ModificationDateTime.ToString("yyyy-MM-dd HH:mm:ss");

            string filePath = GetNormalizedFeedFilePath(feedSaveDirectoryPath, feed);

            if (File.Exists(filePath))
            {
                string ArticleDate = string.Empty;
                string ArticleSummary = string.Empty;
                string ArticleText = string.Empty;
                string ArticleUrl = string.Empty;
                string HeadlineText = string.Empty;
                string RowInsertDateTime = string.Empty;

                using StreamReader LineReader = new(filePath);
                string FileLine = string.Empty;
                string PreviousURL = string.Empty;
                bool InText = false;
                List<string> LineHeaders = new()
                {
                    "URL",
                    "DATE",
                    "HEAD",
                    "TEXT",
                    "SUM",
                };

                while (LineReader.EndOfStream == false && (FileLine = LineReader.ReadLine() ?? string.Empty) is not null)
                {
                    if (string.IsNullOrWhiteSpace(FileLine))
                    {
                        continue;
                    }

                    string Col1 = string.Empty;
                    string Col2 = string.Empty;

                    if (FileLine.Contains(_Tab))
                    {
                        Col1 = FileLine.Substring(0, FileLine.IndexOf(_Tab));
                        Col2 = FileLine.Substring(FileLine.IndexOf(_Tab) + 1);
                    }

                    if (LineHeaders.Contains(Col1))
                    {
                        InText = false;
                    }

                    if (Col1 == "SUM")
                    {
                        ArticleSummary = Col2;
                    }

                    if (Col1 == "TEXT")
                    {
                        InText = true;
                        ArticleText = Col2;
                    }

                    if (LineHeaders.Contains(Col1) == false && InText)
                    {
                        ArticleText += FileLine;
                    }

                    if (Col1 == "URL" && PreviousURL != Col2)
                    {
                        FeedArticle Article = new()
                        {
                            FeedName = feed.FeedName,
                            HeadlineText = HeadlineText,
                            ArticleSummary = ArticleSummary,
                            ArticleText = ArticleText,
                            ArticleDate = ArticleDate,
                            ArticleUrl = ArticleUrl,
                            RowInsertDateTime = RowInsertDateTime
                        };

                        if (!string.IsNullOrWhiteSpace(HeadlineText) && (!string.IsNullOrWhiteSpace(ArticleText) || !string.IsNullOrWhiteSpace(ArticleSummary)))
                        {
                            Articles.Add(Article);
                        }

                        ArticleDate = string.Empty;
                        ArticleSummary = string.Empty;
                        ArticleText = string.Empty;
                        ArticleUrl = Col2;
                        HeadlineText = string.Empty;
                        RowInsertDateTime = ModificationDateTimeText;

                        PreviousURL = Col2;
                    }

                    if (Col1 == "DATE")
                    {
                        ArticleDate = Col2;
                    }

                    if (Col1 == "HEAD")
                    {
                        HeadlineText = Col2;
                    }
                }
            }

            if (Articles.Count > 0)
            {
                string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
                using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);

                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"DATA LAYER INTERFACE {nameof(ImportRSSFeedToDatabase)} Feeds {Feeds.Count} FeedNames {FeedNames.Count} FeedsArticles {FeedsArticles.Count}");
                UpdateRSSTables(feed with { LastRetrieved = ModificationDateTimeText }, Articles, SQLConn);
            }

            return;
        }

        private static string GetNormalizedFeedFilePath(string feedSaveDirectoryPath, Feed feedInfo) => Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.txt");

        private static void UpdateRSSTables(Feed feed, List<FeedArticle> articles, SQLiteConnection sqlConn)
        {
            ModifyFeed(sqlConn, feed);

            foreach (FeedArticle Article in articles)
            {
                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"DATA LAYER INTERFACE {nameof(UpdateRSSTables)} STEP 1 ALL Feed Names {feedNames.Count} then Feed {FeedName} FeedsArticles {FeedArticles.Count}");

                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"DATA LAYER INTERFACE {nameof(UpdateRSSTables)} STEP 2 Headline {article.ArticleDetail.HeadlineText} Summary {article.ArticleDetail.ArticleSummary} Text {article.ArticleDetail.ArticleText} URL {article.ArticleDetail.ArticleUrl}");

                /*Insert or Update feeds_articles table*/
                ModifyFeedArticle(sqlConn, feed, Article);
            }

            return;
        }

        private static void ModifyFeedArticle(SQLiteConnection sqlConn, Feed feedHeader, FeedArticle article)
        {
            bool Exists = FeedArticleReader.Exists(sqlConn, feedHeader.FeedName, article.ArticleUrl);

            if (Exists == false)
            {
                FeedArticleWriter.AddFeedArticle(sqlConn, article);
            }

            else
            {
                FeedArticleWriter.ModifyFeedArticle(sqlConn, article);
            }

            return;
        }

        private static void ModifyFeed(SQLiteConnection sqlConn, Feed feedHeader)
        {
            int Id = feedHeader.DbId;
            bool HasId = Id > 0;
            bool Exists = HasId ?
                FeedReader.Exists(sqlConn, Id) :
                FeedReader.Exists(sqlConn, feedHeader.FeedName);

            if (Exists == false)
            {
                FeedWriter.AddFeed(sqlConn, feedHeader);
            }

            else
            {
                if (HasId)
                {
                    Feed FeedEntry = FeedReader.GetRow(sqlConn, Id);
                    string FeedNameCurrent = FeedEntry.FeedName;
                    string FeedNameProposed = feedHeader.FeedName;

                    if (FeedNameCurrent != FeedNameProposed)
                    {
                        FeedArticleWriter.ModifyFeedArticleKey(sqlConn, FeedNameCurrent, FeedNameProposed);
                    }

                    FeedWriter.ModifyFeedById(sqlConn, feedHeader);
                }

                else
                {
                    FeedWriter.ModifyFeed(sqlConn, feedHeader);
                }
            }

            return;
        }

        public static Feed UpdateFeedConfigurationInDatabase(string feedDbFilePath, Feed feed)
        {
            string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);
            ModifyFeed(SQLConn, feed);
            Feed FeedEntry = FeedReader.GetRow(SQLConn, feed.FeedName);
            return FeedEntry;
        }

        public static bool RemoveFeedFromDatabase(string feedDbFilePath, int feedDbId)
        {
            bool IsDeleted = false;
            string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);
            bool Exists = feedDbId > 0 && FeedReader.Exists(SQLConn, feedDbId);

            /*Only Delete if the Feed record exists in the database.*/
            if (Exists)
            {
                Feed FeedEntry = FeedReader.GetRow(SQLConn, feedDbId);
                FeedArticleWriter.DeleteArticles(SQLConn, FeedEntry.FeedName);
                FeedWriter.DeleteFeedById(SQLConn, feedDbId);
                Exists = FeedReader.Exists(SQLConn, feedDbId);
                IsDeleted = Exists == false;
            }

            return IsDeleted;
        }

        public static void RemoveExpiredArticlesFromDatabase(string sqlConnectionString)
        {
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString);
            FeedArticleWriter.DeleteAllExpiredArticles(SQLConn);
            return;
        }

    }
}
