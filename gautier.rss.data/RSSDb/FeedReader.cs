using System.Data;
using System.Data.SQLite;

namespace gautier.rss.data.RSSDb
{
    public class FeedReader
    {
        private static readonly string _TableName = "feeds";

        public static string TableName
        {
            get => _TableName;
        }

        public static string[] TableColumnNames
        {
            get => new string[]
            {
                "id",
                "feed_name",
                "feed_url",
                "last_retrieved",
                "retrieve_limit_hrs",
                "retention_days",
            };
        }

        public static int CountAllRows(SQLiteConnection sqlConn)
        {
            int Count = 0;
            string CommandText = $"SELECT COUNT(*) FROM {_TableName};";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                Count = Convert.ToInt32(SQLCmd.ExecuteScalar());
            }

            return Count;
        }

        public static int CountRows(SQLiteConnection sqlConn, string feedName)
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

        public static int CountRows(SQLiteConnection sqlConn, int id)
        {
            int Count = 0;
            string CommandText = $"SELECT COUNT(*) FROM {_TableName} WHERE id = @id;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@id", id);
                Count = Convert.ToInt32(SQLCmd.ExecuteScalar());
            }

            return Count;
        }

        public static bool Exists(SQLiteConnection sqlConn, string feedName)
        {
            int Count = CountRows(sqlConn, feedName);
            return Count > 0;
        }

        public static bool Exists(SQLiteConnection sqlConn, int id)
        {
            int Count = CountRows(sqlConn, id);
            return Count > 0;
        }

        internal static void MapSQLParametersToAllTableColumns(SQLiteCommand cmd, Feed FeedHeader, string[] columnNames)
        {
            foreach (string? ColumnName in columnNames)
            {
                string ParamName = $"@{ColumnName}";
                object? ParamValue = string.Empty;

                switch (ColumnName)
                {
                    case "id":
                        ParamValue = FeedHeader.DbId;
                        break;

                    case "feed_name":
                        ParamValue = $"{FeedHeader.FeedName}";
                        break;

                    case "feed_url":
                        ParamValue = $"{FeedHeader.FeedUrl}";
                        break;

                    case "last_retrieved":
                        ParamValue = $"{FeedHeader.LastRetrieved}";
                        break;

                    case "retrieve_limit_hrs":
                        ParamValue = $"{FeedHeader.RetrieveLimitHrs}";
                        break;

                    case "retention_days":
                        ParamValue = $"{FeedHeader.RetentionDays}";
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

        public static Feed GetRow(SQLiteConnection sqlConn, int id)
        {
            Feed FeedEntry = new();
            string CommandText = $"SELECT * FROM {_TableName} WHERE id = @id ORDER BY id;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@id", id);
                using SQLiteDataReader SQLRowReader = SQLCmd.ExecuteReader();
                int ColCount = SQLRowReader.FieldCount;

                while (SQLRowReader.Read())
                {
                    FeedEntry = CreateFeed(SQLRowReader, ColCount);
                }
            }

            return FeedEntry;
        }

        public static Feed GetRow(SQLiteConnection sqlConn, string feedName)
        {
            Feed FeedEntry = new();
            string CommandText = $"SELECT * FROM {_TableName} WHERE feed_name = @FeedName ORDER BY id;";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                SQLCmd.Parameters.AddWithValue("@FeedName", feedName);
                using SQLiteDataReader SQLRowReader = SQLCmd.ExecuteReader();
                int ColCount = SQLRowReader.FieldCount;

                while (SQLRowReader.Read())
                {
                    FeedEntry = CreateFeed(SQLRowReader, ColCount);
                }
            }

            return FeedEntry;
        }

        public static List<Feed> GetAllRows(SQLiteConnection sqlConn)
        {
            List<Feed> Rows = new();
            string CommandText = $"SELECT * FROM {_TableName};";

            using (SQLiteCommand SQLCmd = new(CommandText, sqlConn))
            {
                using (SQLiteDataReader SQLRowReader = SQLCmd.ExecuteReader())
                {
                    int ColCount = SQLRowReader.FieldCount;

                    while (SQLRowReader.Read())
                    {
                        Feed FeedEntry = CreateFeed(SQLRowReader, ColCount);
                        Rows.Add(FeedEntry);
                    }
                }
            }

            return Rows;
        }

        private static Feed CreateFeed(SQLiteDataReader rowReader, int fieldCount)
        {
            int Id = -1;
            string FeedName = string.Empty;
            string FeedUrl = string.Empty;
            string LastRetrieved = string.Empty;
            string RetrieveLimitHrs = string.Empty;
            string RetentionDays = string.Empty;
            string RowInsertDateTime = string.Empty;

            for (int ColI = 0; ColI < fieldCount; ColI++)
            {
                string ColName = rowReader.GetName(ColI);
                object ColValue = rowReader.GetValue(ColI);

                switch (ColName)
                {
                    case "id":
                        Id = rowReader.GetInt32(ColI);
                        break;

                    case "feed_name":
                        FeedName = $"{ColValue}";
                        break;

                    case "feed_url":
                        FeedUrl = $"{ColValue}";
                        break;

                    case "last_retrieved":
                        LastRetrieved = $"{ColValue}";
                        break;

                    case "retrieve_limit_hrs":
                        RetrieveLimitHrs = $"{ColValue}";
                        break;

                    case "retention_days":
                        RetentionDays = $"{ColValue}";
                        break;

                    case "row_insert_date_time":
                        RowInsertDateTime = $"{ColValue}";
                        break;
                }
            }

            return new()
            {
                DbId = Id,
                FeedName = FeedName,
                FeedUrl = FeedUrl,
                LastRetrieved = LastRetrieved,
                RetrieveLimitHrs = RetrieveLimitHrs,
                RetentionDays = RetentionDays,
                RowInsertDateTime = RowInsertDateTime,
            };
        }
    }
}
