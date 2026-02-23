using Microsoft.Data.Sqlite;
using System.Text;

namespace gautier.rss.data.RSSDb
{
    internal class FeedArticleWriter
    {
        private static readonly string[] _ColumnNames = FeedArticleReader.TableColumnNames;

        internal static void AddFeedArticle(SqliteConnection sqlConn, FeedArticle article)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLInsertCMDText(FeedArticleReader.TableName, ColumnNames);

            using (SqliteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedArticleReader.MapSQLParametersToAllTableColumns(SQLCmd, article, ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteArticles(SqliteConnection sqlConn, string feedName)
        {
            string CommandText = $"DELETE FROM {FeedArticleReader.TableName} WHERE feed_name = @FeedName;";

            using (SqliteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteAllExpiredArticles(SqliteConnection sqlConn)
        {
            string CommandText = $"DELETE FROM {FeedArticleReader.TableName} AS a0 WHERE a0.article_url IN " +
                $"(" +
                $"SELECT art.article_url FROM {FeedArticleReader.TableName} AS art " +
                $"INNER JOIN feeds AS fed ON art.feed_name = fed.feed_name " +
                $"WHERE " +
                $"DATETIME('now', 'localtime') > DATETIME(art.row_insert_date_time, '+' || fed.retention_days || ' days')" +
                $"GROUP BY art.article_url" +
                $");";
            using SqliteCommand SQLCmd = new(CommandText, sqlConn);
            SQLCmd.ExecuteNonQuery();
            return;
        }

        internal static void ModifyFeedArticle(SqliteConnection sqlConn, FeedArticle article)
        {
            string[] ColumnNames = SQLUtil.StripColumnNames(new()
            {
                "id", "feed_name", "article_url",
            }, _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedArticleReader.TableName, ColumnNames);
            CommandText.Append("feed_name = @feed_name AND article_url = @article_url;");

            using (SqliteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedArticleReader.MapSQLParametersToAllTableColumns(SQLCmd, article, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeedArticleKey(SqliteConnection sqlConn, string feedNameOld, string feedNameNew)
        {
            string CommandText = $"UPDATE {FeedArticleReader.TableName} SET feed_name = @FeedNameNew WHERE feed_name = @FeedNameOld;";

            using (SqliteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedNameOld", feedNameOld);
                SQLCmd.Parameters.AddWithValue("@FeedNameNew", feedNameNew);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }
    }
}
