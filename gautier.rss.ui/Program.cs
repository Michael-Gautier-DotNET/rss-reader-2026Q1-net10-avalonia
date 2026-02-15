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
            Avalonia.AppBuilder UIContextBuild = AppBuilder.Configure<App>();
            UIContextBuild = AppBuilderDesktopExtensions.UsePlatformDetect(UIContextBuild);
            UIContextBuild = AppBuilderExtension.WithInterFont(UIContextBuild);
            UIContextBuild = LoggingExtensions.LogToTrace(UIContextBuild);

            /*
            Console.WriteLine($"{DateTime.Now}");
            Console.WriteLine("=== Gautier RSS Avalonia Edition ===");
            Console.WriteLine($"Executing from: {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            */

            bool DbIsReady = CheckDbIsReady();

            if (DbIsReady)
            {
                //Console.WriteLine("Starting up ...");
                //Console.WriteLine($"{DateTime.Now}");
                var ExitType = Avalonia.Controls.ShutdownMode.OnLastWindowClose;
                
                ClassicDesktopStyleApplicationLifetimeExtensions.StartWithClassicDesktopLifetime(UIContextBuild, args, ExitType);
            }
            else
            {
                Environment.Exit(1);
            }
        }

        public static bool CheckDbIsReady()
        {
            bool DbIsReady = false;

            try
            {
                //Console.WriteLine($"Database will be created at: {FeedConfiguration.LocalDatabaseLocation}");
                //Console.WriteLine($"Directory exists: {Directory.Exists(Path.GetDirectoryName(FeedConfiguration.LocalDatabaseLocation))}");
                FeedConfiguration.EnsureDatabaseExists();
                //Console.WriteLine("Database initialization completed.");

                // Verify the file was created where expected
                DbIsReady = File.Exists(FeedConfiguration.LocalDatabaseLocation);

                if (!DbIsReady)
                {
                    Console.WriteLine("ERROR: Database file was not created!");
                }
                /*
            else
            {
                FileInfo? fileInfo = new(FeedConfiguration.LocalDatabaseLocation);
                Console.WriteLine($"Database file created: {fileInfo.FullName}");
                Console.WriteLine($"File size: {fileInfo.Length} bytes");
                Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
            }
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL: Database initialization failed");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return DbIsReady;
        }
   }
}
