using System.Data.SQLite;
using System.Text;

namespace gautier.rss.data.RSSDb
{
    internal class FeedArticleWriter
    {
        private static readonly string[] _ColumnNames = FeedArticleReader.TableColumnNames;

        internal static void AddFeedArticle(in SQLiteConnection sqlConn, in FeedArticleUnion article)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLInsertCMDText(FeedArticleReader.TableName, ColumnNames);

            using (SQLiteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedArticleReader.MapSQLParametersToAllTableColumns(SQLCmd, article.ArticleDetail, ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteArticles(in SQLiteConnection sqlConn, in string feedName)
        {
            string CommandText = $"DELETE FROM {FeedArticleReader.TableName} WHERE feed_name = @FeedName;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteAllExpiredArticles(in SQLiteConnection sqlConn)
        {
            string CommandText = $"DELETE FROM {FeedArticleReader.TableName} AS a0 WHERE a0.article_url IN " +
                $"(" +
                $"SELECT art.article_url FROM {FeedArticleReader.TableName} AS art " +
                $"INNER JOIN feeds AS fed ON art.feed_name = fed.feed_name " +
                $"WHERE " +
                $"DATETIME('now', 'localtime') > DATETIME(art.row_insert_date_time, '+' || fed.retention_days || ' days')" +
                $"GROUP BY art.article_url" +
                $");";
            using SQLiteCommand SQLCmd = new(CommandText, sqlConn);
            SQLCmd.ExecuteNonQuery();
            return;
        }

        internal static void ModifyFeedArticle(in SQLiteConnection sqlConn, in FeedArticleUnion article)
        {
            string[] ColumnNames = SQLUtil.StripColumnNames(new()
            {
                "id", "feed_name", "article_url",
            }, _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedArticleReader.TableName, ColumnNames);
            CommandText.Append("feed_name = @feed_name AND article_url = @article_url;");

            using (SQLiteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedArticleReader.MapSQLParametersToAllTableColumns(SQLCmd, article.ArticleDetail, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeedArticleKey(in SQLiteConnection sqlConn, in string feedNameOld, in string feedNameNew)
        {
            string CommandText = $"UPDATE {FeedArticleReader.TableName} SET feed_name = @FeedNameNew WHERE feed_name = @FeedNameOld;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedNameOld", feedNameOld);
                SQLCmd.Parameters.AddWithValue("@FeedNameNew", feedNameNew);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }
    }
}
