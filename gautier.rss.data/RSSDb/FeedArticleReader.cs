using System.Data;
using System.Data.SQLite;

namespace gautier.rss.data.RSSDb
{
    public class FeedArticleReader
    {
        private static readonly string _TableName = "feeds_articles";

        public static string TableName
        {
            get => _TableName;
        }

        public static string[] TableColumnNames
        {
            get =>
            [
                "id",
                "feed_name",
                "headline_text",
                "article_summary",
                "article_text",
                "article_date",
                "article_url",
                "row_insert_date_time",
            ];
        }

        public static int CountAllRows(in SQLiteConnection sqlConn)
        {
            int Count = 0;
            string CommandText = $"SELECT COUNT(*) FROM {_TableName};";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                Count = Convert.ToInt32(SQLCmd.ExecuteScalar());
            }

            return Count;
        }

        public static int CountRows(in SQLiteConnection sqlConn, in string feedName)
        {
            int Count = 0;
            string CommandText = $"SELECT COUNT(*) FROM {_TableName} WHERE feed_name = @FeedName;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);
                Count = Convert.ToInt32(SQLCmd.ExecuteScalar());
            }

            return Count;
        }

        public static int CountRows(in SQLiteConnection sqlConn, in string feedName, in string articleUrl)
        {
            int Count = 0;
            string CommandText = $"SELECT COUNT(*) FROM {_TableName} WHERE feed_name = @FeedName AND article_url = @ArticleUrl;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);
                SQLCmd.Parameters.AddWithValue("@ArticleUrl", articleUrl);
                Count = Convert.ToInt32(SQLCmd.ExecuteScalar());
            }

            return Count;
        }

        public static bool Exists(in SQLiteConnection sqlConn, in string feedName)
        {
            int Count = CountRows(sqlConn, feedName);
            return Count > 0;
        }

        public static bool Exists(in SQLiteConnection sqlConn, in string feedName, in string articleUrl)
        {
            int Count = CountRows(sqlConn, feedName, articleUrl);
            return Count > 0;
        }

        internal static void MapSQLParametersToAllTableColumns(in SQLiteCommand cmd, in FeedArticle article, in string[] columnNames)
        {
            foreach (string? ColumnName in columnNames)
            {
                string ParamName = $"@{ColumnName}";
                object? ParamValue = string.Empty;

                switch (ColumnName)
                {
                    case "id":
                        ParamValue = article.DbId;
                        break;

                    case "feed_name":
                        ParamValue = $"{article.FeedName}";
                        break;

                    case "headline_text":
                        ParamValue = $"{article.HeadlineText}";
                        break;

                    case "article_summary":
                        ParamValue = $"{article.ArticleSummary}";
                        break;

                    case "article_text":
                        ParamValue = $"{article.ArticleText}";
                        break;

                    case "article_date":
                        ParamValue = $"{article.ArticleDate}";
                        break;

                    case "article_url":
                        ParamValue = $"{article.ArticleUrl}";
                        break;

                    case "row_insert_date_time":
                        ParamValue = $"{article.RowInsertDateTime}";
                        break;
                }

                SQLiteParameter Param = cmd.Parameters.AddWithValue(ParamName, ParamValue);

                if (ColumnName == "id")
                {
                    Param.DbType = DbType.Int32;
                }
            }

            return;
        }

        public static List<FeedArticle> GetAllRows(in SQLiteConnection sqlConn)
        {
            List<FeedArticle> Rows = new();
            string CommandText = $"SELECT * FROM {_TableName};";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                using (SQLiteDataReader SQLRowReader = SQLCmd.ExecuteReader())
                {
                    CollectRows(SQLRowReader, Rows);
                }
            }

            return Rows;
        }

        public static List<FeedArticle> GetRows(in SQLiteConnection sqlConn, in string feedName)
        {
            List<FeedArticle> Rows = new();
            string CommandText = $"SELECT * FROM {_TableName} WHERE feed_name = @FeedName;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);

                using (SQLiteDataReader SQLRowReader = SQLCmd.ExecuteReader())
                {
                    CollectRows(SQLRowReader, Rows);
                }
            }

            return Rows;
        }

        private static void CollectRows(in SQLiteDataReader reader, in List<FeedArticle> rows)
        {
            int ColCount = reader.FieldCount;

            while (reader.Read())
            {
                int Id = -1;
                string FeedName = string.Empty;
                string HeadlineText = string.Empty;
                string ArticleSummary = string.Empty;
                string ArticleText = string.Empty;
                string ArticleDate = string.Empty;
                string ArticleUrl = string.Empty;
                string RowInsertDateTime = string.Empty;

                for (int ColI = 0; ColI < ColCount; ColI++)
                {
                    string ColName = reader.GetName(ColI);
                    object ColValue = reader.GetValue(ColI);

                    switch (ColName)
                    {
                        case "id":
                            Id = reader.GetInt32(ColI);
                            break;

                        case "feed_name":
                            FeedName = $"{ColValue}";
                            break;

                        case "headline_text":
                            HeadlineText = $"{ColValue}";
                            break;

                        case "article_summary":
                            ArticleSummary = $"{ColValue}";
                            break;

                        case "article_text":
                            ArticleText = $"{ColValue}";
                            break;

                        case "article_date":
                            ArticleDate = $"{ColValue}";
                            break;

                        case "article_url":
                            ArticleUrl = $"{ColValue}";
                            break;

                        case "row_insert_date_time":
                            RowInsertDateTime = $"{ColValue}";
                            break;
                    }
                }

                FeedArticle FeedEntry = new()
                {
                    DbId = Id,
                    FeedName = FeedName,
                    HeadlineText = HeadlineText,
                    ArticleSummary = ArticleSummary,
                    ArticleText = ArticleText,
                    ArticleDate = ArticleDate,
                    ArticleUrl = ArticleUrl,
                    RowInsertDateTime = RowInsertDateTime,
                };
                rows.Add(FeedEntry);
            }

            return;
        }
    }
}
