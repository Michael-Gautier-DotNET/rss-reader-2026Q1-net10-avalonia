using System.Collections.ObjectModel;
using System.Globalization;

using Avalonia;

using gautier.rss.data;

namespace gautier.rss.ui.UIData
{
    public class BindableFeed : AvaloniaObject
    {
        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;

        public static readonly StyledProperty<int> IdProperty =
            AvaloniaProperty.Register<BindableFeed, int>("Id", -1);

        public static readonly StyledProperty<string> NameProperty =
            AvaloniaProperty.Register<BindableFeed, string>("Name", string.Empty);

        public static readonly StyledProperty<string> UrlProperty =
            AvaloniaProperty.Register<BindableFeed, string>("Url", string.Empty);

        public static readonly StyledProperty<DateTime> LastRetrievedProperty =
            AvaloniaProperty.Register<BindableFeed, DateTime>("LastRetrieved", DateTime.Now);

        public static readonly StyledProperty<int> RetrieveLimitHrsProperty =
            AvaloniaProperty.Register<BindableFeed, int>("RetrieveLimitHrs", 1);

        public static readonly StyledProperty<int> RetentionDaysProperty =
            AvaloniaProperty.Register<BindableFeed, int>("RetentionDays", 45);

        public int Id
        {
            get => GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public string Name
        {
            get => GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public string OriginalName { get; set; } = string.Empty;

        public string Url
        {
            get => GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public string OriginalUrl { get; set; } = string.Empty;

        public DateTime LastRetrieved
        {
            get => GetValue(LastRetrievedProperty);
            set => SetValue(LastRetrievedProperty, value);
        }

        public int RetrieveLimitHrs
        {
            get => GetValue(RetrieveLimitHrsProperty);
            set => SetValue(RetrieveLimitHrsProperty, value);
        }

        public int RetentionDays
        {
            get => GetValue(RetentionDaysProperty);
            set => SetValue(RetentionDaysProperty, value);
        }

        internal static ObservableCollection<BindableFeed> ConvertFeeds(in SortedList<string, Feed> feeds)
        {
            ObservableCollection<BindableFeed>? BFeeds = new();

            foreach (Feed? FeedEntry in feeds.Values)
            {
                BindableFeed? BFeed = ConvertFeed(FeedEntry);
                BFeeds.Add(BFeed);
            }

            return BFeeds;
        }

        internal static List<Feed> ConvertFeeds(in ObservableCollection<BindableFeed> feeds)
        {
            List<Feed>? DFeeds = new();

            foreach (BindableFeed? BFeed in feeds)
            {
                Feed? DFeed = ConvertFeed(BFeed);
                DFeeds.Add(DFeed);
            }

            return DFeeds;
        }

        internal static Feed ConvertFeed(in BindableFeed feed)
        {
            return new()
            {
                DbId = feed.Id,
                FeedName = feed.Name,
                FeedUrl = feed.Url,
                LastRetrieved = feed.LastRetrieved.ToString(_InvariantFormat.UniversalSortableDateTimePattern),
                RetrieveLimitHrs = $"{feed.RetrieveLimitHrs}",
                RetentionDays = $"{feed.RetentionDays}",
            };
        }

        internal static BindableFeed ConvertFeed(in Feed feed)
        {
            return new()
            {
                Id = feed.DbId,
                Name = feed.FeedName,
                Url = feed.FeedUrl,
                OriginalName = feed.FeedName,
                OriginalUrl = feed.FeedUrl,
                LastRetrieved = DateTime.Parse(feed.LastRetrieved),
                RetrieveLimitHrs = int.Parse(feed.RetrieveLimitHrs),
                RetentionDays = int.Parse(feed.RetentionDays),
            };
        }

        internal static BindableFeed ConvertFeedNarrow(in Feed feed)
        {
            return new()
            {
                Id = feed.DbId,
                Name = feed.FeedName,
                Url = feed.FeedUrl,
                LastRetrieved = DateTime.Parse(feed.LastRetrieved),
                RetrieveLimitHrs = int.Parse(feed.RetrieveLimitHrs),
                RetentionDays = int.Parse(feed.RetentionDays),
            };
        }
    }
}
