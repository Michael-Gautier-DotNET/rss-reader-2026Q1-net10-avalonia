using System.Data.SQLite;
using System.Globalization;
using System.Text;

using gautier.rss.data.FeedXml;
using gautier.rss.data.RSSDb;

namespace gautier.rss.data
{
    public static class FeedFileConverter
    {
        private const char _Tab = '\t';

        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;

        /// <summary>
        /// Designed to generate static local files even if they are later accidentally deleted.
        /// </summary>
        public static void CreateStaticFeedFiles(string feedSaveDirectoryPath, string feedDbFilePath, Feed[] feedInfos)
        {
            if (Directory.Exists(feedSaveDirectoryPath) == false)
            {
                Directory.CreateDirectory(feedSaveDirectoryPath);
            }

            string ConnectionString = SQLUtil.GetSQLiteConnectionString(feedDbFilePath, 3);
            using SQLiteConnection SQLConn = SQLUtil.OpenSQLiteConnection(ConnectionString);

            /*
             * At this point, in this version, the feed rows are statically fed through the software application.
             * NOTE TO SELF ... change this to drive exclusively off "both" a simple configuration file and existing database.
             *      In the future the way it will work is the program will look for the existence of an RSS database.
             *          If one exists, then it will load the feed entries into memory and append any entries from a file
             *            that does not already exist in the in-memory representation.
             *          If an rss db in SQLite format does not exist, then it will be created. The file entries will
             *            populate the database (as it currently does in the late June/early July 2023 design).
             *
             *          The primary focus of these design points is to assist future development and troubleshooting.
             *
             *          The UI version of this program will not have these requirements since RSS feed configuration will be
             *             driven by a UI screen and not subject to self-sustained start-up automation as is the case here.
             */
            foreach (Feed? FeedInfo in feedInfos)
            {
                string FeedFilePath = FeedFileUtil.GetRSSXmlFeedFilePath(feedSaveDirectoryPath, FeedInfo);
                /*
                 * VERY IMPORTANT
                 *      This code path is the central nexus that determines if the entire program
                 *         is a naive implementation versus an actual RSS implementation.
                 *         Feeds should update at regular intervals without causing issues
                 *         related to accessing the source websites too often.
                 *
                 *  Shortcut:  The upstream input should have modified the in-memory
                 *      representation of the feed entry to contain the relevant flags.
                 */
                bool Exists = FeedReader.Exists(SQLConn, FeedInfo.FeedName);
                /*
                 * Default assumption is the file should be created when there is no file.
                 * This should be overriden however when the logic shows a feed was already
                 *   accessed over the network within the upper time limit of 60 minutes.
                 */
                bool ShouldCacheFileBeCreated = File.Exists(FeedFilePath) == false;

                /*
                 * An RSS feed database table entry exists and should be used to determine
                 *    if the feed should be retrieved versus reusing a local cache file.
                 */
                if (Exists)
                    /*
                     * Go into a complex logic cycle.
                     *      The main goal is to access the feed based on the columns:
                                "last_retrieved",
                                "retrieve_limit_hrs"

                        Remember to set the ShouldCacheFileBeCreated = false when
                            there is an indication a feed website was recently accessed.
                        Even when there is no local file but the feed was accessed over the network,
                        that situation must still be respected to avoid future access issues.
                     */
                {
                    bool FeedCanBeUpdated = RSSNetClient.CheckFeedIsEligibleForUpdate(FeedInfo);

                    if (FeedCanBeUpdated)
                    {
                        Console.WriteLine(@"********* Feed released for update.");

                        if (File.Exists(FeedFilePath) == false)
                        {
                            Console.WriteLine(@"********* Feed cache file will be created.");
                            ShouldCacheFileBeCreated = true;
                        }
                    }
                }

                /*
                 * A cached file may not exist.
                 * Indicates a situation where a feed table entry does not exist and no file exists.
                 *      That can happen during testing, development and modification of the system.
                 *      In all cases, re-do the local cache files. This will also trigger regeneration
                 *      of the feed table entry after the transformation/translation stage.
                 */
                if (ShouldCacheFileBeCreated)
                {
                    Console.WriteLine($"Creating a Cached XML file for feed: {FeedInfo.FeedName} {FeedInfo.FeedUrl}");
                    Console.WriteLine($"\t\t\t{FeedFilePath}");
                    RSSNetClient.CreateRSSFeedFile(FeedInfo.FeedUrl, FeedFilePath);
                }
            }

            return;
        }

        public static void TransformStaticFeedFiles(string feedSaveDirectoryPath, Feed[] feeds)
        {
            SortedList<string, List<FeedArticle>> Feeds = TransformXmlFeedToFeedArticles(feedSaveDirectoryPath, feeds);

            foreach (Feed? FeedInfo in feeds)
            {
                if (Feeds.ContainsKey(FeedInfo.FeedName) == false)
                {
                    continue;
                }

                WriteRSSArticlesToFile(feedSaveDirectoryPath, FeedInfo, Feeds[FeedInfo.FeedName]);
            }

            return;
        }

        public static string WriteRSSArticlesToFile(string feedSaveDirectoryPath, Feed feed, List<FeedArticle> articles)
        {
            StringBuilder RSSFeedFileOutput = new();
            string NormalizedFeedFilePath = FeedFileUtil.GetRSSTabDelimitedFeedFilePath(feedSaveDirectoryPath, feed);

            using (StreamWriter RSSFeedFile = new(NormalizedFeedFilePath, false))
            {
                foreach (FeedArticle? Article in articles)
                {
                    string HeadlineText = Article.HeadlineText;
                    string ArticleSummary = Article.ArticleSummary;
                    string ArticleText = Article.ArticleText;
                    string ArticleDate = Article.ArticleDate;
                    string ArticleUrl = Article.ArticleUrl;
                    RSSFeedFileOutput.AppendLine($"URL{_Tab}{ArticleUrl}");
                    RSSFeedFileOutput.AppendLine($"DATE{_Tab}{ArticleDate}");
                    RSSFeedFileOutput.AppendLine($"HEAD{_Tab}{HeadlineText}");
                    RSSFeedFileOutput.AppendLine($"TEXT{_Tab}{ArticleText}");
                    RSSFeedFileOutput.AppendLine($"SUM{_Tab}{ArticleSummary}");
                }

                RSSFeedFile.Write(RSSFeedFileOutput.ToString());
                RSSFeedFile.Flush();
                RSSFeedFile.Close();
                Console.WriteLine($"\t\tMade TXT tab-delimited cached file | {feed.FeedName}:");
                Console.WriteLine($"\t\t\t\t{NormalizedFeedFilePath}");
            }

            RSSFeedFileOutput.Clear();
            return NormalizedFeedFilePath;
        }

        public static SortedList<string, List<FeedArticle>> TransformXmlFeedToFeedArticles(string feedSaveDirectoryPath, Feed[] feeds)
        {
            SortedList<string, List<FeedArticle>> FeedArticles = new();

            foreach (Feed? FeedInfo in feeds)
            {
                List<FeedArticle> Articles = TransformXmlFeedToFeedArticles(feedSaveDirectoryPath, FeedInfo);
                FeedArticles[FeedInfo.FeedName] = Articles;
            }

            return FeedArticles;
        }

        public static List<FeedArticle> TransformXmlFeedToFeedArticles(string feedSaveDirectoryPath, Feed feed)
        {
            List<FeedArticle> Articles = new();
            string RSSFeedFilePath = FeedFileUtil.GetRSSXmlFeedFilePath(feedSaveDirectoryPath, feed);
            XFeed RSSFeed = RSSNetClient.CreateRSSXFeed(RSSFeedFilePath);

            foreach (XArticle? RSSItem in RSSFeed.Articles)
            {
                FeedArticle FeedItem = CreateRSSFeedArticle(feed, RSSItem);
                Articles.Add(FeedItem);
            }

            return Articles;
        }

        private static FeedArticle CreateRSSFeedArticle(Feed feed, XArticle article)
        {
            string ArticleText = article.ContentEncoded;
            string ArticleUrl = article.Link;
            DateTime ArticleDate;
            DateTime.TryParse(article.PublicationDate, out ArticleDate);
            string ArticleDateText = ArticleDate.ToString(_InvariantFormat.UniversalSortableDateTimePattern);
            return new()
            {
                FeedName = feed.FeedName,
                HeadlineText = article.Title,
                ArticleSummary = article.Description,
                ArticleText = ArticleText,
                ArticleDate = ArticleDateText,
                ArticleUrl = ArticleUrl,
                RowInsertDateTime = DateTime.Now.ToString(_InvariantFormat.UniversalSortableDateTimePattern),
            };
        }

        public static Feed[] GetStaticFeedInfos(string feedsFilePath)
        {
            List<Feed> Feeds = new();
            SortedList<string, string> FeedByName = new();

            using (StreamReader FeedsReader = new(feedsFilePath))
            {
                while (FeedsReader.EndOfStream == false)
                {
                    string FeedLine = $"{FeedsReader.ReadLine()}";

                    if (string.IsNullOrWhiteSpace(FeedLine))
                    {
                        continue;
                    }

                    string[] Columns = FeedLine.Split(_Tab);

                    if (Columns.Length < 1)
                    {
                        continue;
                    }

                    string FeedName = Columns[0];
                    string FeedUrl = Columns[1];
                    /*
                     * If there are duplicate feed entries, the structure of the collection
                     *   is defined such that the most recent entry prevails.
                     */
                    FeedByName[FeedName] = FeedUrl;
                }
            }

            foreach (string FeedName in FeedByName.Keys)
            {
                Feed FeedEntry = new()
                {
                    FeedName = FeedName,
                    FeedUrl = FeedByName[FeedName],
                };
                Feeds.Add(FeedEntry);
            }

            return Feeds.ToArray();
        }
    }
}
