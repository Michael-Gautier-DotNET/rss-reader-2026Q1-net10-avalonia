using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using gautier.rss.data;
using gautier.rss.ui.UIData;

namespace gautier.rss.ui
{
    public partial class MainWindow : Window
    {
        private static readonly DateTimeFormatInfo _InvariantFormat = DateTimeFormatInfo.InvariantInfo;
        private DateTime _LastExpireCheck = DateTime.Now;
        private List<Feed> _Feeds = new();
        private List<Feed> _FeedsBefore = new();

        private readonly TimeSpan _FeedUpdateInterval = TimeSpan.FromMinutes(10);
        private DispatcherTimer _FeedUpdateTimer;

        private static readonly string _EmptyArticle = "No article content available.";

        private TabItem ReaderTab
        {
            get => ReaderTabs?.SelectedItem as TabItem;
        }

        private ListBox FeedHeadlines
        {
            get => ReaderTab?.Content as ListBox;
        }

        private FeedArticle Article
        {
            get => (FeedArticle)FeedHeadlines.SelectedItem;//Explicitly fail if not type
        }

        public MainWindow() => InitializeComponent();

        private void Window_Initialized(object sender, EventArgs e)
        {
            PruneExpiredArticles();

            _Feeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);

            _FeedUpdateTimer = new()
            {
                Interval = _FeedUpdateInterval
            };

            _FeedUpdateTimer.Tick += UpdateFeedsOnInterval;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}");
            Console.WriteLine($"Refresh interval {_FeedUpdateInterval.ToString()}");
            Console.WriteLine($"************************** {DateTime.Now.ToString("dddd   MMMM \t\tMM/dd/yyyy tt")}");
            Console.WriteLine($"************************** ************************** \t{DateTime.Now.ToString("yyyy-MM-dd hh:mmmm:ss tt")}");

            UpdateFeedTabs();
        }

        private async void UpdateFeedsOnInterval(object sender, EventArgs e)
        {
            _FeedUpdateTimer.Stop();

            _FeedUpdateTimer.Interval = _FeedUpdateInterval;

            await AcquireFeedsAsync();
        }

        private async Task AcquireFeedsAsync()
        {
            await DownloadFeedsAsync();

            lock (_FeedUpdateTimer)
            {
                FeedDataExchange.RemoveExpiredArticlesFromDatabase(FeedConfiguration.SQLiteDbConnectionString);

                _FeedsBefore = _Feeds;

                _Feeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);
            }
            await Dispatcher.UIThread.InvokeAsync(UpdateFeedTabs);
        }

        private async Task DownloadFeedsAsync()
        {
            List<Feed> DbFeeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);

            DateTime RecentDateTime = DateTime.Now;
            Console.WriteLine($"\t UI {nameof(DownloadFeedsAsync)} [{DbFeeds.Count}] Feeds");

            foreach (Feed FeedEntry in DbFeeds)
            {
                bool RetrieveLimitIsValid = int.TryParse(FeedEntry.RetrieveLimitHrs, out int RetrieveLimitHrs);
                bool LastRetrievedFormatIsValid = DateTime.TryParseExact(FeedEntry.LastRetrieved, "yyyy-MM-dd HH:mm:ss", _InvariantFormat, DateTimeStyles.None, out DateTime LastRetrievedDateTime);
                DateTime FeedRenewalDateTime = LastRetrievedDateTime.AddHours(RetrieveLimitHrs);

                bool FeedIsEligibleForUpdate = RecentDateTime > FeedRenewalDateTime;

                Console.WriteLine("**************************");
                Console.WriteLine($"************************** ************************** \t{RecentDateTime.ToString("yyyy-MM-dd hh:mmmm:ss tt")}");
                Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} Processing {FeedEntry.FeedName}");
                Console.WriteLine($"\t\t\t /// Recent Date: {RecentDateTime}");
                Console.WriteLine($"\t\t\t /// Update Frequency: {FeedEntry.RetrieveLimitHrs} Hrs");
                Console.WriteLine($"\t\t\t /// Retention Days: {FeedEntry.RetentionDays}");
                Console.WriteLine($"\t\t\t /// Retrieve Limit Is Valid: {RetrieveLimitIsValid}");
                Console.WriteLine($"\t\t\t /// Last RetrievedFormat Is Valid: {LastRetrievedFormatIsValid}");
                Console.WriteLine($"\t\t\t /// Feed Is Eligible For Update: {FeedIsEligibleForUpdate}");
                Console.WriteLine($"\t\t\t /// {FeedEntry.FeedUrl}");
                Console.WriteLine($"\t\t\t /// Last Retrieved: {LastRetrievedDateTime}");
                Console.WriteLine($"\t\t\t /// Feed Renewal Date: {FeedRenewalDateTime}");

                Console.WriteLine("************************** ************************** **************************");

                FeedDataExchange.DownloadFeed(FeedEntry, FeedConfiguration.LocalRootFilesLocation, FeedConfiguration.LocalDatabaseLocation);
            }

            return;
        }

        private void PruneExpiredArticles()
        {
            DateTime nextExpireCheck = _LastExpireCheck.AddHours(1);

            if (DateTime.Now > nextExpireCheck)
            {
                _LastExpireCheck = DateTime.Now;
                FeedDataExchange.RemoveExpiredArticlesFromDatabase(FeedConfiguration.SQLiteDbConnectionString);
            }
        }

        private async void UpdateFeedTabs()
        {
            ReaderManagerButton.IsEnabled = false;
            try
            {
                SyncTabs();
            }
            finally
            {
                ReaderManagerButton.IsEnabled = true;

                _FeedUpdateTimer.Start();
            }
        }

        private void SyncTabs()
        {
            RemoveOrphanedTabs();

            //Add new tabs
            foreach (Feed FeedEntry in _Feeds)
            {
                TabItem FeedTab = GetTab(FeedEntry);

                if (FeedTab is null)
                {
                    FeedTab = AddRSSTab(FeedEntry);
                }

                AddArticles(FeedEntry, FeedTab);
            }
        }

        private void RemoveOrphanedTabs()
        {
            //Sometimes a person wants to delete all their feeds in one shot	    
            if (_Feeds.Count == 0 && ReaderTabs.Items.Count > 0)
            {
                ReaderTabs.Items.Clear();
            }

            //Remove tabs that are no longer in the database
            foreach (Feed FeedEntry in _FeedsBefore)
            {
                TabItem FeedTab = GetTab(FeedEntry);

                if (FeedTab is not null && _Feeds.Where(n => n.DbId == FeedEntry.DbId).FirstOrDefault() is Feed ActiveFeedEntry)
                {
                    UpdateRSSTab(FeedEntry, ActiveFeedEntry, FeedTab);
                }
                else
                {
                    FeedTab = GetTab(FeedEntry);
                    ReaderTabs.Items.Remove(FeedTab);
                }
            }

            _FeedsBefore.Clear();
        }

        private TabItem AddRSSTab(Feed feed)
        {
            ListBox Contents = new()
            {
                Background = Brushes.Transparent,
                BorderThickness = new(0),
                FontSize = 16,
                ItemsSource = new ObservableCollection<FeedArticle>()
            };

            App.SetDisplayMemberPath(Contents, "HeadlineText");
            Contents.SelectionChanged += Headline_SelectionChanged;

            TabItem Tab = new()
            {
                Header = feed.FeedName,
                Tag = feed,
                Content = Contents,
            };

            ReaderTabs.Items.Add(Tab);

            return Tab;
        }

        private TabItem UpdateRSSTab(Feed previous, Feed updated, TabItem tab)
        {
            int Changed = 0;

            if (string.Equals(updated.FeedName, previous.FeedName, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                tab.Header = updated.FeedName;
                Changed++;
            }
            else if (string.Equals(updated.FeedUrl, previous.FeedUrl, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                Changed++;
            }

            if (Changed > 0)
            {
                tab.Tag = updated;
            }

            return tab;
        }

        private TabItem GetTab(Feed feed)
        {
            TabItem Tab = null;

            foreach (TabItem tab in ReaderTabs.Items)
            {
                if (tab.Tag is Feed FeedEntry && FeedEntry.DbId == feed.DbId)
                {
                    Tab = tab;
                    break;
                }
            }

            return Tab;
        }

        private void AddArticles(Feed feedEntry, TabItem tab)
        {
            if ((tab.Content as ListBox)?.ItemsSource is ObservableCollection<FeedArticle> EffectiveArticles)
            {
                var LastIDLocal = EffectiveArticles.Any() ? EffectiveArticles.Max(n => n.DbId) : 0;

                var LastIDDb = FeedDataExchange.GetMaxArticleID(FeedConfiguration.SQLiteDbConnectionString, feedEntry.FeedName);

                if (LastIDDb > LastIDLocal)
                {
                    List<FeedArticle> LatestArticles = FeedDataExchange.GetFeedArticles(FeedConfiguration.SQLiteDbConnectionString, feedEntry.FeedName, LastIDLocal + 1, LastIDDb);

                    foreach (FeedArticle ArticleEntry in LatestArticles)
                    {
                        if (!EffectiveArticles.Any(n => n.ArticleUrl == ArticleEntry.ArticleUrl))
                        {
                            EffectiveArticles.Add(ArticleEntry);
                        }
                    }
                    //FeedContent.ItemsSource = EffectiveArticles;
                    //UpdateLayout();
                }

                SelectDefaultArticle();
            }
        }

        //Updates the right-side of the screen
        //Does not modify the list on the left-side
        //Works as designed
        private void ApplyArticle(FeedArticle article)
        {
            static string GetText(FeedArticle v) =>
                v switch
                {
                    _ when !string.IsNullOrWhiteSpace(v.ArticleText) => v.ArticleText,
                    _ when !string.IsNullOrWhiteSpace(v.ArticleSummary) => v.ArticleSummary,
                    _ => _EmptyArticle
                };

            ReaderArticleText.Text = ConvertHtmlToPlainText(GetText(article));

            ReaderHeadline.Content = article.HeadlineText.Trim();
        }

        private void SelectDefaultArticle()
        {
            if (FeedHeadlines.SelectedIndex < 0 && FeedHeadlines.Items.Count > 0)
            {
                FeedHeadlines.SelectedIndex = 0;
            }
            else if (FeedHeadlines.SelectedIndex > -1)
            {
                ApplyArticle(Article);
            }
        }

        private string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return "No article content available.";
            }

            string text = System.Net.WebUtility.HtmlDecode(html);
            // Replace common block elements with newlines
            text = text.Replace("</p>", "\n\n")
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("</div>", "\n")
                .Replace("</li>", "\n")
                .Replace("</h1>", "\n\n")
                .Replace("</h2>", "\n\n")
                .Replace("</h3>", "\n\n")
                .Replace("</h4>", "\n\n")
                .Replace("</h5>", "\n\n")
                .Replace("</h6>", "\n\n");
            // Remove all HTML tags
            text = RemoveHtmlTags(text);
            // Clean up whitespace
            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, @"\n\s*\n", "\n\n");
            text = text.Trim();
            return text;
        }

        private string RemoveHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            char[] array = new char[html.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < html.Length; i++)
            {
                char c = html[i];

                if (c == '<')
                {
                    inside = true;
                    continue;
                }

                if (c == '>')
                {
                    inside = false;
                    continue;
                }

                if (!inside)
                {
                    array[arrayIndex] = c;
                    arrayIndex++;
                }
            }

            return new(array, 0, arrayIndex);
        }

        private void ReaderArticleLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Article.ArticleUrl))
            {
                string url = Article.ArticleUrl;
                ProcessStartInfo startInfo = new()
                {
                    UseShellExecute = true,
                    FileName = url,
                };

                try
                {
                    Process.Start(startInfo);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening URL: {ex.Message}");
                }
            }
        }

        private void Headline_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyArticle(Article);

        private void ReaderTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReaderTab is null)
            {
                return;
            }

            ReaderFeedName?.Content = $"{ReaderTab.Header}";

            SelectDefaultArticle();
        }

        private void ReaderManagerButton_Click(object sender, RoutedEventArgs e)
        {
            _FeedUpdateTimer.Stop();

            RSSManagerUI managerWindow = new();

            managerWindow.Closed += (localSender, localEventArgs) => RSSManagerClosed();

            managerWindow.ShowDialog(this);
        }

        private void RSSManagerClosed()
        {
            _FeedUpdateTimer.Interval = TimeSpan.FromSeconds(0.2);
            _FeedUpdateTimer.Start();
        }
    }
}
