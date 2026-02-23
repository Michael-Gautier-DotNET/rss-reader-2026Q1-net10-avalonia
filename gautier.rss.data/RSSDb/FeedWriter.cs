using Microsoft.Data.Sqlite;
using System.Text;

namespace gautier.rss.data.RSSDb
{
    internal class FeedWriter
    {
        private static readonly string[] _ColumnNames = FeedReader.TableColumnNames;

        internal static void AddFeed(SqliteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLInsertCMDText(FeedReader.TableName, ColumnNames);

            using (SqliteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeedById(SqliteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedReader.TableName, ColumnNames);
            CommandText.Append("id = @id;");

            using (SqliteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeed(SqliteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnNames(new()
            {
                "id", "feed_name",
            }, _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedReader.TableName, ColumnNames);
            CommandText.Append("feed_name = @feed_name;");

            using (SqliteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteFeedById(SqliteConnection sqlConn, int id)
        {
            string CommandText = $"DELETE FROM {FeedReader.TableName} WHERE id = @Id;";

            using (SqliteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@Id", id);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }
    }
}
