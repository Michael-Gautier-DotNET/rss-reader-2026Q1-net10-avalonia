using System.Data.SQLite;
using System.Globalization;

using gautier.rss.data.RSSDb;

namespace gautier.rss.data
{
    public static class FeedDataExchange
    {
        private const char _Tab = '\t';

        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;

        public static SortedList<string, Feed> GetAllFeeds(string sqlConnectionString)
        {
            SortedList<string, Feed> Feeds = new();

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                List<Feed> FeedEntries = FeedReader.GetAllRows(SQLConn);

                foreach (Feed FeedEntry in FeedEntries)
                {
                    Feeds[FeedEntry.FeedName] = FeedEntry;
                }
            }

            return Feeds;
        }

        public static SortedList<string, SortedList<string, FeedArticle>> GetAllFeedArticles(string sqlConnectionString)
        {
            SortedList<string, SortedList<string, FeedArticle>> Articles = new(100);

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                List<FeedArticle> FeedArticles = FeedArticleReader.GetAllRows(SQLConn);

                foreach (FeedArticle ArticleEntry in FeedArticles)
                {
                    string FeedName = ArticleEntry.FeedName;

                    if (Articles.ContainsKey(FeedName) == false)
                    {
                        Articles[FeedName] = new(1000);
                    }

                    if (Articles.ContainsKey(FeedName))
                    {
                        string ArticleUrl = ArticleEntry.ArticleUrl;
                        SortedList<string, FeedArticle> ArticlesByUrl = Articles[FeedName];

                        if (ArticlesByUrl.ContainsKey(ArticleUrl) == false)
                        {
                            ArticlesByUrl.Add(ArticleUrl, ArticleEntry);
                        }
                    }
                }
            }

            return Articles;
        }

        public static SortedList<string, FeedArticle> GetFeedArticles(string sqlConnectionString, string feedName)
        {
            SortedList<string, FeedArticle> Articles = new(100);

            using (SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(sqlConnectionString))
            {
                List<FeedArticle> FeedArticles = FeedArticleReader.GetRows(SQLConn, feedName);

                foreach (FeedArticle ArticleEntry in FeedArticles)
                {
                    string ArticleUrl = ArticleEntry.ArticleUrl;

                    if (Articles.ContainsKey(ArticleUrl) == false)
                    {
                        Articles.Add(ArticleUrl, ArticleEntry);
                    }
                }
            }

            return Articles;
        }

        public static void ImportStaticFeedFilesToDatabase(string feedSaveDirectoryPath, string feedDbFilePath, Feed[] feeds)
        {
            SortedList<string, List<FeedArticleUnion>> FeedsArticles = new();

            foreach (Feed FeedEntry in feeds)
            {
                List<FeedArticleUnion> Articles = ImportRSSFeedToDatabase(feedSaveDirectoryPath, feedDbFilePath, FeedEntry);
                FeedsArticles[FeedEntry.FeedName] = Articles;
            }

            Console.WriteLine($"\t\tUpdated SQLite database | {feedDbFilePath}");
            return;
        }

        public static List<FeedArticleUnion> ImportRSSFeedToDatabase(string feedSaveDirectoryPath, string feedDbFilePath, Feed feed)
        {
            List<FeedArticleUnion> Articles = new();
            DateTime ModificationDateTime = DateTime.Now;
            string ModificationDateTimeText = ModificationDateTime.ToString(_InvariantFormat.UniversalSortableDateTimePattern);
            feed.LastRetrieved = ModificationDateTimeText;
            feed.RetrieveLimitHrs = "1";
            feed.RetentionDays = "45";
            string filePath = GetNormalizedFeedFilePath(feedSaveDirectoryPath, feed);

            if (File.Exists(filePath))
            {
                using StreamReader LineReader = new(filePath);
                string FileLine = string.Empty;
                string PreviousURL = string.Empty;
                FeedArticleUnion FeedArticlePair = new();
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
                        FeedArticlePair.ArticleDetail.ArticleSummary = Col2;
                    }

                    if (Col1 == "TEXT")
                    {
                        InText = true;
                        FeedArticlePair.ArticleDetail.ArticleText = Col2;
                    }

                    if (LineHeaders.Contains(Col1) == false && InText)
                    {
                        FeedArticlePair.ArticleDetail.ArticleText += FileLine;
                    }

                    if (Col1 == "URL" && PreviousURL != Col2)
                    {
                        FeedArticlePair = new()
                        {
                            FeedHeader = feed,
                            ArticleDetail = new()
                            {
                                FeedName = feed.FeedName,
                            },
                        };
                        FeedArticlePair.ArticleDetail.RowInsertDateTime = ModificationDateTimeText;
                        Articles.Add(FeedArticlePair);
                        PreviousURL = Col2;
                    }

                    if (Col1 == "URL")
                    {
                        FeedArticlePair.ArticleDetail.ArticleUrl = Col2;
                    }

                    if (Col1 == "DATE")
                    {
                        FeedArticlePair.ArticleDetail.ArticleDate = Col2;
                    }

                    if (Col1 == "HEAD")
                    {
                        FeedArticlePair.ArticleDetail.HeadlineText = Col2;
                    }
                }
            }

            if (Articles.Count > 0)
            {
                SortedList<string, List<FeedArticleUnion>> FeedsArticles = new()
                {
                    [feed.FeedName] = Articles,
                };
                List<string> FeedNames = new()
                {
                    feed.FeedName,
                };
                SortedList<string, Feed> Feeds = new()
                {
                    [feed.FeedName] = feed,
                };
                string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
                using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);

                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"DATA LAYER INTERFACE {nameof(ImportRSSFeedToDatabase)} Feeds {Feeds.Count} FeedNames {FeedNames.Count} FeedsArticles {FeedsArticles.Count}");
                UpdateRSSTables(FeedsArticles, SQLConn, FeedNames, Feeds);
            }

            return Articles;
        }

        private static string GetNormalizedFeedFilePath(string feedSaveDirectoryPath, Feed feedInfo) => Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.txt");

        public static void WriteRSSArticlesToDatabase(string feedDbFilePath, SortedList<string, List<FeedArticleUnion>> feedsArticles)
        {
            string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);
            IList<string>? FeedNames = feedsArticles.Keys;
            SortedList<string, Feed> Feeds = CollectFeeds(feedsArticles, FeedNames);
            /*Insert or Update feeds and feeds_articles tables*/
            UpdateRSSTables(feedsArticles, SQLConn, FeedNames, Feeds);
            return;
        }

        private static SortedList<string, Feed> CollectFeeds(SortedList<string, List<FeedArticleUnion>> feedsArticles, IList<string> feedNames)
        {
            SortedList<string, Feed> Feeds = new();

            foreach (string? FeedName in feedNames)
            {
                if (Feeds.ContainsKey(FeedName) == false)
                {
                    List<FeedArticleUnion>? FUL = feedsArticles[FeedName];

                    if (FUL.Count > 0)
                    {
                        FeedArticleUnion? FU = FUL[0];
                        Feeds[FeedName] = FU.FeedHeader;
                    }
                }
            }

            return Feeds;
        }

        private static void UpdateRSSTables(SortedList<string, List<FeedArticleUnion>> feedsArticles, SQLiteConnection sqlConn, IList<string> feedNames, SortedList<string, Feed> feeds)
        {
            foreach (string? FeedName in feedNames)
            {
                Feed? FeedHeader = feeds[FeedName];
                List<FeedArticleUnion> FeedArticles = feedsArticles[FeedName];
                /*Insert or Update feeds table*/
                ModifyFeed(sqlConn, FeedHeader);

                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"DATA LAYER INTERFACE {nameof(UpdateRSSTables)} STEP 1 ALL Feed Names {feedNames.Count} then Feed {FeedName} FeedsArticles {FeedArticles.Count}");

                foreach (FeedArticleUnion? article in FeedArticles)
                {
                    /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                    //Console.WriteLine($"DATA LAYER INTERFACE {nameof(UpdateRSSTables)} STEP 2 Headline {article.ArticleDetail.HeadlineText} Summary {article.ArticleDetail.ArticleSummary} Text {article.ArticleDetail.ArticleText} URL {article.ArticleDetail.ArticleUrl}");

                    /*Insert or Update feeds_articles table*/
                    ModifyFeedArticle(sqlConn, FeedHeader, article);
                }
            }

            return;
        }

        private static void ModifyFeedArticle(SQLiteConnection sqlConn, Feed feedHeader, FeedArticleUnion article)
        {
            bool Exists = FeedArticleReader.Exists(sqlConn, feedHeader.FeedName, article.ArticleDetail.ArticleUrl);

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

        public static Feed[] MergeFeedEntries(string feedDbFilePath, Feed[] feedEntries)
        {
            string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);
            List<string> FeedUrls = new();
            List<Feed> StaticFeedEntries = new(feedEntries);
            List<Feed> FeedEntries = FeedReader.GetAllRows(SQLConn);

            if (FeedEntries.Count > 0)
            {
                /*Feed entries from the database.*/
                MergeValidateFeedEntries(FeedEntries, StaticFeedEntries, FeedUrls);
                /*Feed entries from the file.*/
                MergeValidateFeedEntries(StaticFeedEntries, FeedEntries, FeedUrls);
            }

            else
            {
                FeedEntries = StaticFeedEntries;
            }

            return FeedEntries.ToArray();
        }

        private static void MergeValidateFeedEntries(List<Feed> leftSideValues, List<Feed> rightSideValues, List<string> secondKeys)
        {
            foreach (Feed LeftEntry in leftSideValues)
            {
                string SecondKey = LeftEntry.FeedUrl.ToLower();

                if (secondKeys.Contains(SecondKey))
                {
                    continue;
                }

                else
                {
                    secondKeys.Add(SecondKey);
                }

                /*Feed entries from the file.*/
                foreach (Feed RightSideEntry in rightSideValues)
                {
                    if (LeftEntry.FeedName == RightSideEntry.FeedName)
                    {
                        string RightSideSecondKey = RightSideEntry.FeedUrl.ToLower();

                        if (SecondKey != RightSideSecondKey && secondKeys.Contains(RightSideSecondKey) == false)
                        {
                            /*
                             * Give another feed the opportunity to use the url under a different feed name.
                             * I do not really support this functionality but I am annotating the concept regardless.
                             */
                            secondKeys.Remove(RightSideSecondKey);
                            /*
                             * For now, assume the url from the secondary source is more current.
                             *      In the future,
                             *          may want to compare the
                             *              file's last modifed time to the data record's last retrieved time.
                             */
                            LeftEntry.FeedUrl = RightSideSecondKey;
                            secondKeys.Add(RightSideSecondKey);
                        }
                    }
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
