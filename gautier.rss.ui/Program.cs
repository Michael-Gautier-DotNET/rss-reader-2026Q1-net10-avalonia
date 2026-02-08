using System.Reflection;

using Avalonia;

using gautier.rss.ui.UIData;

namespace gautier.rss.ui
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Gautier RSS Avalonia Edition ===");
            Console.WriteLine($"Executing from: {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");

            try
            {
                // Log the database path before initialization
                Console.WriteLine($"Database will be created at: {FeedConfiguration.LocalDatabaseLocation}");
                Console.WriteLine(
                    $"Directory exists: {Directory.Exists(Path.GetDirectoryName(FeedConfiguration.LocalDatabaseLocation))}");
                // Initialize database
                FeedConfiguration.EnsureDatabaseExists();
                Console.WriteLine("✅ Database initialization completed.");

                // Verify the file was created where expected
                if (File.Exists(FeedConfiguration.LocalDatabaseLocation))
                {
                    FileInfo? fileInfo = new(FeedConfiguration.LocalDatabaseLocation);
                    Console.WriteLine($"✅ Database file created: {fileInfo.FullName}");
                    Console.WriteLine($"✅ File size: {fileInfo.Length} bytes");
                    Console.WriteLine($"✅ Last modified: {fileInfo.LastWriteTime}");
                }

                else
                {
                    Console.WriteLine("❌ ERROR: Database file was not created!");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"❌ FATAL: Database initialization failed");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }

            Console.WriteLine("Starting up ...");
            // Now start the Avalonia UI
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
