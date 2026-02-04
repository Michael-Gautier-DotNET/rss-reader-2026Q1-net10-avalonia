namespace gautier.rss.data
{
    public static class FeedFileUtil
    {
        public static string GetRSSXmlFeedFilePath(in string feedSaveDirectoryPath, in Feed feedInfo)
        {
            return Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.xml");
        }

        public static string GetRSSTabDelimitedFeedFilePath(in string feedSaveDirectoryPath, in Feed feedInfo)
        {
            return Path.Combine(feedSaveDirectoryPath, $"{feedInfo.FeedName}.txt");
        }

        public static bool CheckSourceFileNewer(in string sourceFilePath, in string comparisonFilePath)
        {
            FileInfo SourceFile = new(sourceFilePath);
            FileInfo ComparisonFile = new(comparisonFilePath);
            bool Newer = SourceFile.LastWriteTimeUtc > ComparisonFile.LastWriteTimeUtc;
            return Newer;
        }
    }
}
