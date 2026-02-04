using System.Data.SQLite;

namespace gautier.rss.data.RSSDb
{
    public static class DbInitializer
    {
        public static void EnsureDatabaseExists(in string dbFilePath)
        {
            Console.WriteLine($"Ensuring database at: {dbFilePath}");
            // Ensure directory exists
            string directory = Path.GetDirectoryName(dbFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Console.WriteLine($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            // Check if we need to create schema
            bool needsSchema = !File.Exists(dbFilePath);

            using (SQLiteConnection? connection = new($"Data Source={dbFilePath};Version=3;"))
            {
                connection.Open();
                Console.WriteLine($"Database opened. State: {connection.State}");

                if (needsSchema)
                {
                    Console.WriteLine("Creating new database schema...");
                    CreateSchema(connection);
                }

                else
                {
                    Console.WriteLine("Database exists. Verifying schema...");
                    VerifyAndRepairSchema(connection);
                }
            }

            Console.WriteLine($"Database initialization complete. File size: {new FileInfo(dbFilePath).Length} bytes");
        }

        private static void CreateSchema(in SQLiteConnection connection)
        {
            Console.WriteLine("Creating Schema...");

            // Use a single transaction for all operations
            using (SQLiteTransaction? transaction = connection.BeginTransaction())
            {
                try
                {
                    // Create feeds table
                    ExecuteNonQuery(connection, transaction, @"
                        CREATE TABLE feeds (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            feed_name TEXT UNIQUE NOT NULL,
                            feed_url TEXT NOT NULL,
                            last_retrieved TEXT NOT NULL,
                            retrieve_limit_hrs TEXT NOT NULL,
                            retention_days TEXT NOT NULL
                        )");
                    Console.WriteLine("Created 'feeds' table");
                    // Create feeds_articles table
                    ExecuteNonQuery(connection, transaction, @"
                        CREATE TABLE feeds_articles (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            feed_name TEXT NOT NULL,
                            article_url TEXT NOT NULL,
                            article_date TEXT NOT NULL,
                            headline_text TEXT NOT NULL,
                            article_text TEXT,
                            article_summary TEXT,
                            row_insert_date_time TEXT NOT NULL
                        )");
                    Console.WriteLine("Created 'feeds_articles' table");
                    // Create indexes
                    ExecuteNonQuery(connection, transaction, @"
                        CREATE INDEX idx_feeds_articles_feed_name 
                        ON feeds_articles(feed_name)");
                    ExecuteNonQuery(connection, transaction, @"
                        CREATE INDEX idx_feeds_articles_article_url 
                        ON feeds_articles(article_url)");
                    Console.WriteLine("Created indexes");
                    transaction.Commit();
                    Console.WriteLine("Schema Transaction Committed");
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CreateSchema: {ex.Message}");
                    transaction.Rollback();
                    Console.WriteLine("Schema Transaction Rollback");
                    throw;
                }
            }
        }

        private static void ExecuteNonQuery(in SQLiteConnection connection, in SQLiteTransaction transaction, in string sql)
        {
            using (SQLiteCommand? command = new(sql, connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void VerifyAndRepairSchema(in SQLiteConnection connection)
        {
            Console.WriteLine("Verifying schema...");
            // Simple check: count expected tables
            string checkTables = @"
                SELECT COUNT(*) FROM sqlite_master 
                WHERE type='table' AND name IN ('feeds', 'feeds_articles')";

            using (SQLiteCommand? command = new(checkTables, connection))
            {
                int tableCount = Convert.ToInt32(command.ExecuteScalar());
                Console.WriteLine($"Found {tableCount} required tables");

                if (tableCount < 2)
                {
                    Console.WriteLine("Schema incomplete. Repairing...");
                    CreateSchema(connection);
                }

                else
                {
                    Console.WriteLine("Schema verified OK");
                }
            }
        }
    }
}
