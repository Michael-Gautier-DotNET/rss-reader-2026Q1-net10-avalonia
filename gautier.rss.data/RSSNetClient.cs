using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

using gautier.rss.data.FeedXml;

using RestSharp;

namespace gautier.rss.data
{
    /// <summary>
    /// Handles network communication. Translates rss XML data into C# object format.
    /// </summary>
    public static class RSSNetClient
    {
        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;

        public static bool CheckFeedIsEligibleForUpdate(in Feed feed)
        {
            bool FeedIsEligibleForUpdate = false;
            bool RetrieveLimitIsValid = int.TryParse(feed.RetrieveLimitHrs, out int RetrieveLimitHrs);

            if (RetrieveLimitIsValid)
            {
                bool LastRetrievedFormatIsValid = DateTime.TryParseExact(feed.LastRetrieved, _InvariantFormat.UniversalSortableDateTimePattern, _InvariantFormat, DateTimeStyles.None, out DateTime LastRetrievedDateTime);

                if (LastRetrievedFormatIsValid)
                {
                    DateTime FeedRenewalDateTime = LastRetrievedDateTime.AddHours(RetrieveLimitHrs);
                    DateTime RecentDateTime = DateTime.Now;
                    Console.WriteLine($"Feed: {feed.FeedName} | Feed Renewal Date: {FeedRenewalDateTime} vs Recent Date: {RecentDateTime}");
                    Console.WriteLine($"\t\tUpdate Frequency: {feed.RetrieveLimitHrs} Hrs | Retention Days: {feed.RetentionDays} | Last Retrieved: {LastRetrievedDateTime}");
                    FeedIsEligibleForUpdate = RecentDateTime > FeedRenewalDateTime;
                }
            }

            return FeedIsEligibleForUpdate;
        }

        public static string DownloadFeed(in string fileDownloadDirectoryPath, in Feed feed)
        {
            string FilePath = Path.Combine(fileDownloadDirectoryPath, $"{feed.FeedName}.xml");
            bool FeedCanBeUpdated = CheckFeedIsEligibleForUpdate(feed);

            if (FeedCanBeUpdated)
            {
                string Url = feed.FeedUrl;
                RestClient HttpHandle = new(Url);
                RestRequest HttpRequest = new();
                RestResponse HttpResponse = HttpHandle.Execute(HttpRequest);
                string? Content = HttpResponse.Content;

                if (string.IsNullOrWhiteSpace(Content) == false)
                {
                    File.WriteAllText(FilePath, Content);
                }
            }

            return FilePath;
        }

        public static void CreateRSSFeedFile(in string feedUrl, in string rssFeedFilePath)
        {
            try
            {
                using XmlReader feedXml = XmlReader.Create(feedUrl);
                using XmlWriter feedXmlWriter = XmlWriter.Create(rssFeedFilePath);
                feedXmlWriter.WriteNode(feedXml, false);
            }

            catch (HttpRequestException httpE)
            {
                Console.WriteLine(httpE.Message);
            }

            return;
        }

        public static XFeed CreateRSSXFeed(in string rssFeedFilePath)
        {
            XFeed RSSFeed = new();
            bool FeedFileExists = File.Exists(rssFeedFilePath);

            if (FeedFileExists)
            {
                XFeedParser Parser = new();
                RSSFeed = Parser.ParseFile(rssFeedFilePath);
            }

            return RSSFeed;
        }

        public static bool ValidateUrlIsHttpOrHttps(in string UrlValue)
        {
            bool IsValidUrl = false;

            if (string.IsNullOrEmpty(UrlValue) == false)
            {
                IsValidUrl = ValidateUrlIsHttpOrHttpsRegEx(UrlValue);

                /*
                 * Validate using Uri class.
                 */
                if (IsValidUrl == false)
                {
                    bool InitialCheck = Uri.IsWellFormedUriString(UrlValue, UriKind.Absolute);

                    if (InitialCheck)
                    {
                        IsValidUrl = ValidateUrlIsHttpOrHttpsURI(UrlValue);
                    }
                }

                if (IsValidUrl == false)
                {
                    IsValidUrl = ValidateUrlIsHttpOrHttpsText(UrlValue);
                }
            }

            return IsValidUrl;
        }

        public static bool ValidateUrlIsHttpOrHttpsText(in string url)
        {
            bool IsValidUrl = false;
            /*Check protocol scheme*/
            bool InitialCheck = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            string Host = string.Empty;

            /*Protocol scheme is valid. Obtain Host.*/
            if (InitialCheck)
            {
                int DoubleSlashIndex = url.IndexOf("//", StringComparison.OrdinalIgnoreCase);
                int HostStartIndex = DoubleSlashIndex + 2;
                int HostEndIndex = url.IndexOf("/", HostStartIndex, StringComparison.OrdinalIgnoreCase);

                if (HostEndIndex == -1)
                {
                    HostEndIndex = url.Length;
                }

                Host = url[HostStartIndex..HostEndIndex];
            }

            /*Host is valid, check the remainder of the url.*/
            if (InitialCheck && string.IsNullOrEmpty(Host) == false)
            {
                List<char> ValidChars = new()
                {
                    '.',//Dot
                    '-',//Dash
                    '_',//Underscore
                };
                int ValidCharCount = 0;

                foreach (char Character in Host)
                {
                    if (char.IsLetterOrDigit(Character) || ValidChars.Contains(Character))
                    {
                        ValidCharCount++;
                    }
                }

                IsValidUrl = ValidCharCount == url.Length;
            }

            return IsValidUrl;
        }

        public static bool ValidateUrlIsHttpOrHttpsRegEx(in string url)
        {
            /*
             * Validate with Regular Expression.
             */
            string ValidationPattern = @"^(https?://)[^\s/$.?#].[^\s]*$";
            bool IsValidUrl = Regex.IsMatch(url, ValidationPattern);
            return IsValidUrl;
        }

        public static bool ValidateUrlIsHttpOrHttpsURI(in string url)
        {
            bool IsUri = Uri.TryCreate(url, UriKind.Absolute, out Uri? UriValue);
            bool IsHttpOrHttps = false;

            if (IsUri)
            {
                IsHttpOrHttps = UriValue?.Scheme == Uri.UriSchemeHttp || UriValue?.Scheme == Uri.UriSchemeHttps;
            }

            return IsHttpOrHttps;
        }

    }
}
