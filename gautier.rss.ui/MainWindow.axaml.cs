using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
        private DateTime _LastExpireCheck = DateTime.Now;
        private SortedList<string, Feed> _Feeds = new();
        private SortedList<string, Feed> _FeedsBefore = new();

        private readonly TimeSpan _QuickTimeSpan = TimeSpan.FromSeconds(0.7);
        private readonly TimeSpan _MidTimeSpan = TimeSpan.FromSeconds(7);
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
            _FeedUpdateTimer = new()
            {
                Interval = _QuickTimeSpan,
            };

            _FeedUpdateTimer.Tick += UpdateFeedsOnInterval;
            _FeedUpdateTimer.Start();
        }

        private async void UpdateFeedsOnInterval(object sender, EventArgs e)
        {
            _FeedUpdateTimer.Stop();

            _FeedUpdateTimer.Interval = _MidTimeSpan;

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
            PruneExpiredArticles();
            SortedList<string, Feed> DbFeeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);

            static void ExecuteDownload(Feed FeedEntry)
            {
                string RSSXmlFilePath = RSSNetClient.DownloadFeed(FeedConfiguration.LocalRootFilesLocation, FeedEntry);

                /*Leave these quick diagnostic statements. They are useful in a pinch.*/
                //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} {RSSXmlFilePath}");

                if (File.Exists(RSSXmlFilePath))
                {
                    string RSSIntegrationFilePath =
                        FeedFileUtil.GetRSSTabDelimitedFeedFilePath(FeedConfiguration.LocalRootFilesLocation, FeedEntry);

                    //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} {RSSIntegrationFilePath}");

                    List<FeedArticle> Articles =
                        FeedFileConverter.TransformXmlFeedToFeedArticles(FeedConfiguration.LocalRootFilesLocation,
                            FeedEntry);

                    //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} saved to: {FeedConfiguration.LocalRootFilesLocation}");

                    string RSSTabDelimitedFilePath =
                        FeedFileConverter.WriteRSSArticlesToFile(FeedConfiguration.LocalRootFilesLocation, FeedEntry,
                            Articles);

                    //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} delimited in: {FeedConfiguration.LocalRootFilesLocation}");

                    bool RSSIntegrationPathIsValid = RSSIntegrationFilePath == RSSTabDelimitedFilePath;

                    //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} integration path valid: {RSSIntegrationPathIsValid}");
                    if (RSSIntegrationPathIsValid && File.Exists(RSSTabDelimitedFilePath))
                    {
                        //Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {FeedEntry.FeedName} {FeedEntry.FeedUrl} DATABASE IMPORT {RSSTabDelimitedFilePath}");
                        FeedDataExchange.ImportRSSFeedToDatabase(FeedConfiguration.LocalRootFilesLocation,
                            FeedConfiguration.LocalDatabaseLocation, FeedEntry);
                    }
                }
            }

            Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} {DbFeeds.Count} Feeds");

            foreach (Feed FeedEntry in DbFeeds.Values)
            {
                Console.WriteLine($"\t\t UI {nameof(DownloadFeedsAsync)} Processing {FeedEntry.FeedName} {FeedEntry.FeedUrl} Last Retrieved {FeedEntry.LastRetrieved}");

                ExecuteDownload(FeedEntry);
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
            foreach (Feed FeedEntry in _Feeds.Values)
            {
                var FeedUrl = FeedEntry.FeedUrl;

                var Exists = GetTabByUrl(FeedUrl) is not null;

                if (Exists == false)
                {
                    TabItem FeedTab = AddRSSTab(FeedEntry);

                    AddArticles(FeedEntry, FeedTab);
                }
            }

            if (FeedHeadlines.SelectedIndex < 0 && FeedHeadlines.Items.Count > 0)
            {
                FeedHeadlines.SelectedIndex = 0;
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
            foreach (Feed FeedEntry in _FeedsBefore.Values)
            {
                var FeedUrl = FeedEntry.FeedUrl;

                var Exists = true;

                foreach (Feed ActiveFeedEntry in _Feeds.Values)
                {
                    Exists = (ActiveFeedEntry.FeedUrl == FeedUrl);

                    if (Exists)
                    {
                        /**
                    		Name syncronization
                    		This is an extra function but is convenient to place here.
                    	**/
                        string HeaderLeft = ActiveFeedEntry.FeedName;
                        string HeaderRight = FeedEntry.FeedName;

                        if (HeaderLeft != HeaderRight)
                        {
                            TabItem FeedTab = GetTabByUrl(FeedUrl);

                            FeedTab.Header = HeaderLeft;
                        }

                        break;
                    }
                }

                if (Exists == false)
                {
                    TabItem FeedTab = GetTabByUrl(FeedUrl);
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
                Tag = feed.FeedUrl,
                Content = Contents,
            };

            ReaderTabs.Items.Add(Tab);

            return Tab;
        }

        private TabItem GetTabByUrl(string url)
        {
            TabItem Tab = null;

            foreach (TabItem tab in ReaderTabs.Items)
            {
                var Found = $"{tab.Tag}" == url;

                if (Found)
                {
                    Tab = tab;
                    break;
                }
            }

            return Tab;
        }

        private void AddArticles(Feed feedEntry, TabItem tab)
        {
            SortedList<string, FeedArticle> AllArticles = FeedDataExchange.GetFeedArticles(FeedConfiguration.SQLiteDbConnectionString, feedEntry.FeedName);

            List<string> AllArticleUrls = new(AllArticles.Keys);

            if ((tab.Content as ListBox)?.ItemsSource is ObservableCollection<FeedArticle> EffectiveArticles)
            {
                foreach (string ArticleUrl in AllArticleUrls)
                {
                    bool Exists = ContainsArticleUrl(EffectiveArticles, ArticleUrl);

                    if (!Exists)
                    {
                        FeedArticle Article = AllArticles[ArticleUrl];
                        EffectiveArticles.Add(Article);
                    }
                }

                var FeedContent = tab.Content as ListBox;

                FeedContent.ItemsSource = EffectiveArticles;
                FeedContent.UpdateLayout();
            }
        }

        private static bool ContainsArticleUrl(IList<FeedArticle> articles, string articleUrl)
        {
            foreach (FeedArticle article in articles)
            {
                if (string.Equals(article.ArticleUrl, articleUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

            if (FeedHeadlines.SelectedIndex > -1)
            {
                Headline_SelectionChanged(sender, e);
            }
            else if (FeedHeadlines.Items.Count > 0)
            {
                FeedHeadlines.SelectedIndex = 0;
            }

            return;
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
            _FeedUpdateTimer.Interval = _QuickTimeSpan;
            _FeedUpdateTimer.Start();
        }
    }
}
