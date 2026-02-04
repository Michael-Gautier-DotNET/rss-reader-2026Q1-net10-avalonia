using System.Data.SQLite;
using System.Text;

namespace gautier.rss.data.RSSDb
{
    internal class FeedWriter
    {
        private static readonly string[] _ColumnNames = FeedReader.TableColumnNames;

        internal static void AddFeed(SQLiteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLInsertCMDText(FeedReader.TableName, ColumnNames);

            using (SQLiteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeedById(SQLiteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnByName("id", _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedReader.TableName, ColumnNames);
            CommandText.Append("id = @id;");

            using (SQLiteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void ModifyFeed(SQLiteConnection sqlConn, Feed feedHeader)
        {
            string[] ColumnNames = SQLUtil.StripColumnNames(new()
            {
                "id", "feed_name",
            }, _ColumnNames);
            StringBuilder CommandText = SQLUtil.CreateSQLUpdateCMDText(FeedReader.TableName, ColumnNames);
            CommandText.Append("feed_name = @feed_name;");

            using (SQLiteCommand SQLCmd = new(CommandText.ToString(), sqlConn))
            {
                FeedReader.MapSQLParametersToAllTableColumns(SQLCmd, feedHeader, _ColumnNames);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }

        internal static void DeleteFeedById(SQLiteConnection sqlConn, int id)
        {
            string CommandText = $"DELETE FROM {FeedReader.TableName} WHERE id = @Id;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@Id", id);
                SQLCmd.ExecuteNonQuery();
            }

            return;
        }
    }
}
