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

        public static bool CheckFeedIsEligibleForUpdate(Feed feed)
        {
            bool FeedIsEligibleForUpdate = false;
            bool RetrieveLimitIsValid = int.TryParse(feed.RetrieveLimitHrs, out int RetrieveLimitHrs);

            //Console.WriteLine($"\t\t\t --- Retrieve Limit Is Valid: {RetrieveLimitIsValid}");
            if (RetrieveLimitIsValid)
            {
                bool LastRetrievedFormatIsValid = DateTime.TryParseExact(feed.LastRetrieved, "yyyy-MM-dd HH:mm:ss", _InvariantFormat, DateTimeStyles.None, out DateTime LastRetrievedDateTime);

                //Console.WriteLine($"\t\t\t --- Last RetrievedFormat Is Valid: {LastRetrievedFormatIsValid}");

                if (LastRetrievedFormatIsValid)
                {
                    DateTime RecentDateTime = DateTime.Now;
                    DateTime FeedRenewalDateTime = LastRetrievedDateTime.AddHours(RetrieveLimitHrs);
                    /*
                    Console.WriteLine($"\t\t\t --- Recent Date: {RecentDateTime}");
                    Console.WriteLine($"\t\t\t --- Update Frequency: {feed.RetrieveLimitHrs} Hrs");
                    Console.WriteLine($"\t\t\t --- Last Retrieved: {LastRetrievedDateTime}");
                    Console.WriteLine($"\t\t\t --- Feed Renewal Date: {FeedRenewalDateTime}");
		    */
                    FeedIsEligibleForUpdate = RecentDateTime > FeedRenewalDateTime;
                }
            }

            //Console.WriteLine($"\t\t\t --- Feed Is Eligible For Update: {FeedIsEligibleForUpdate}");

            return FeedIsEligibleForUpdate;
        }

        public static bool DownloadFeed(string localDestinationFilePath, Feed feed)
        {
            bool DownloadIsValid = false;
            //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~");
            //Console.WriteLine($"~~~~~~~~~~~~~~~~~~~~~~~~~~ ~~~~~~~~~~~~~~~~~~~~~~~~~~ \t{DateTime.Now.ToString("yyyy-MM-dd hh:mmmm:ss tt")}");

            bool FeedCanBeUpdated = CheckFeedIsEligibleForUpdate(feed);

            //Console.WriteLine($"\t\t {nameof(DownloadFeed)} Processing {feed.FeedName} ** Can Be Updated {FeedCanBeUpdated}");

            if (FeedCanBeUpdated)
            {
                string Url = feed.FeedUrl;
                RestClient HttpHandle = new(Url);
                RestRequest HttpRequest = new();
                RestResponse HttpResponse = HttpHandle.Execute(HttpRequest);
                string? Content = HttpResponse.Content;

                DownloadIsValid = !string.IsNullOrWhiteSpace(Content);
                //Console.WriteLine($"{nameof(DownloadFeed)} Download Success {DownloadIsValid}");
                //Console.WriteLine($"{nameof(DownloadFeed)} Content Length {Content?.Length}");
                if (DownloadIsValid)
                {
                    //Console.WriteLine($"\t\t\t --- {feed.FeedUrl}");
                    //Console.WriteLine($"\t\t\t --- Content written {localDestinationFilePath}");
                    File.WriteAllText(localDestinationFilePath, Content);
                }
            }

            //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~ ~~~~~~~~~~~~~~~~~~~~~~~~~~ ~~~~~~~~~~~~~~~~~~~~~~~~~~");

            return DownloadIsValid;
        }

        public static void CreateRSSFeedFile(string feedUrl, string rssFeedFilePath)
        {
            try
            {
                using var feedXml = XmlReader.Create(feedUrl);
                using var feedXmlWriter = XmlWriter.Create(rssFeedFilePath);
                feedXmlWriter.WriteNode(feedXml, false);
            }

            catch (HttpRequestException httpE)
            {
                Console.WriteLine(httpE.Message);
            }

            return;
        }

        public static XFeed CreateRSSXFeed(string rssFeedFilePath)
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

        public static bool ValidateUrlIsHttpOrHttps(string UrlValue)
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

        public static bool ValidateUrlIsHttpOrHttpsText(string url)
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

        public static bool ValidateUrlIsHttpOrHttpsRegEx(string url)
        {
            /*
             * Validate with Regular Expression.
             */
            string ValidationPattern = @"^(https?://)[^\s/$.?#].[^\s]*$";
            bool IsValidUrl = Regex.IsMatch(url, ValidationPattern);
            return IsValidUrl;
        }

        public static bool ValidateUrlIsHttpOrHttpsURI(string url)
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
