namespace gautier.rss.data
{
    public static class FeedFileUtil
    {
        public static string GetRSSXmlFeedFilePath(string feedSaveDirectoryPath, Feed feedInfo)
        {
            return Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.xml");
        }

        public static string GetRSSTabDelimitedFeedFilePath(string feedSaveDirectoryPath, Feed feedInfo)
        {
            return Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.txt");
        }

        public static bool CheckSourceFileNewer(string sourceFilePath, string comparisonFilePath)
        {
            FileInfo SourceFile = new(sourceFilePath);
            FileInfo ComparisonFile = new(comparisonFilePath);
            bool Newer = SourceFile.LastWriteTimeUtc > ComparisonFile.LastWriteTimeUtc;
            return Newer;
        }
    }
}
