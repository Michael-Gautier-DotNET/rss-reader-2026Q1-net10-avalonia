using System.Reflection;

using gautier.rss.data.RSSDb;

namespace gautier.rss.ui.UIData
{
    internal static class FeedConfiguration
    {
        private static string GetDatabasePath()
        {
            // Get the directory where the executable is running
            string? executableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(executableDir, "rss.db");
        }

        internal static string FeedSaveDirectoryPath
        {
            get => Directory.GetCurrentDirectory();
        }

        internal static string FeedDbFilePath
        {
            get => GetDatabasePath();
        }

        internal static string SQLiteDbConnectionString
        {
            get => SQLUtil.GetSQLiteConnectionString(FeedDbFilePath, 3);
        }

        internal static void EnsureDatabaseExists()
        {
            Console.WriteLine($"Database path: {FeedDbFilePath}");
            Console.WriteLine($"File exists: {File.Exists(FeedDbFilePath)}");
            DbInitializer.EnsureDatabaseExists(FeedDbFilePath);
        }
    }
}
