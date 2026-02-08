using System.Reflection;

using gautier.rss.data.RSSDb;

namespace gautier.rss.ui.UIData
{
    internal static class FeedConfiguration
    {
        internal static string LocalDatabaseLocation
        {
            get
            {
                var DbLocation = Path.Combine(LocalRootFilesLocation, "rss.db");

                return DbLocation;
            }
        }

        internal static string LocalRootFilesLocation
        {
            get => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        internal static string SQLiteDbConnectionString
        {
            get => SQLUtil.GetSQLiteConnectionString(LocalDatabaseLocation, 3);
        }

        internal static void EnsureDatabaseExists()
        {
            Console.WriteLine($"Database path: {LocalDatabaseLocation}");
            Console.WriteLine($"File exists: {File.Exists(LocalDatabaseLocation)}");
            DbInitializer.EnsureDatabaseExists(LocalDatabaseLocation);
        }
    }
}
