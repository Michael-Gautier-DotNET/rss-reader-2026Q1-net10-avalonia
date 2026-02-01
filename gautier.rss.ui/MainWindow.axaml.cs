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
        private readonly List<TabItem> _ReaderTabItems = new();

        private bool _FeedsInitialized = false;
        private DateTime _LastExpireCheck = DateTime.Now;
        private SortedList<string, Feed> _Feeds = null;
        private SortedList<string, FeedArticle> _FeedsArticles = null;
        private int _FeedIndex = -1;

        private readonly TimeSpan _QuickTimeSpan = TimeSpan.FromSeconds(2.34);
        private readonly TimeSpan _MidTimeSpan = TimeSpan.FromSeconds(21.7);
        private DispatcherTimer _FeedUpdateTimer;
        private readonly BackgroundWorker _FeedUpdateTask = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _FeedUpdateTimer = new()
            {
                Interval = _QuickTimeSpan,
            };
            _FeedUpdateTimer.Tick += UpdateFeedsOnInterval;
            _FeedUpdateTask.DoWork += FeedUpdateTask_DoWork;
            _FeedUpdateTask.RunWorkerCompleted += FeedUpdateTask_RunWorkerCompleted;
            _FeedUpdateTimer.Start();
        }

        private string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return "No article content available.";
            }

            string? text = System.Net.WebUtility.HtmlDecode(html);
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

            char[]? array = new char[html.Length];
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

        private void AddRSSTab(string name)
        {
            ListBox? listBox = new()
            {
                Background = Brushes.Transparent,
                BorderThickness = new(0),
                FontSize = 12,
            };
            App.SetDisplayMemberPath(listBox, "HeadlineText");
            listBox.SelectionChanged += Headline_SelectionChanged;
            TabItem? tabItem = new()
            {
                Header = name,
                Content = listBox,
            };
            _ReaderTabItems.Add(tabItem);
            ReaderTabs.Items.Add(tabItem);
        }

        private TabItem FindRSSFeedTab(string name)
        {
            foreach (TabItem tab in ReaderTabs.Items)
            {
                if (tab.Header?.ToString() == name)
                {
                    return tab;
                }
            }

            return null;
        }

        private bool IsFeedIndexValid
        {
            get => _FeedIndex > -1 && _FeedIndex < _ReaderTabItems.Count;
        }

        private TabItem ReaderTab
        {
            get => IsFeedIndexValid ? _ReaderTabItems[_FeedIndex] : null;
        }

        private ListBox FeedHeadlines
        {
            get => ReaderTab?.Content as ListBox;
        }

        private FeedArticle Article
        {
            get => FeedHeadlines?.SelectedItem as FeedArticle;
        }

        private void ReaderArticleLaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (Article != null && !string.IsNullOrEmpty(Article.ArticleUrl))
            {
                string? url = Article.ArticleUrl;
                ProcessStartInfo? startInfo = new()
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

        private void ReaderManagerButton_Click(object sender, RoutedEventArgs e)
        {
        	if(_FeedsInitialized) {
			_FeedUpdateTimer?.Stop();
			RSSManagerUI? managerWindow = new();
			managerWindow.ShowDialog(this);
			CheckRSSManagerUIUpdates(managerWindow);
			_FeedUpdateTimer?.Start();
            	}
        }

        private void Headline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyArticle(Article);
        }

        private void ReaderTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_FeedsInitialized)
            {
                return;
            }

            _FeedIndex = ReaderTabs.SelectedIndex;
            ApplyFeed();
        }

        private void ApplyArticle(in FeedArticle article)
        {
            static string GetText(in FeedArticle v) {
            	return 
            		  !string.IsNullOrWhiteSpace(v?.ArticleText) 
	    		  ? v.ArticleText 
	    		  : !string.IsNullOrWhiteSpace(v?.ArticleSummary) 
	    		  ? v.ArticleSummary 
	    		  : "No article content available." 
	    		  ; 
	    }

            ReaderArticleText.Text = ConvertHtmlToPlainText(GetText(article));

	    ReaderHeadline.Content = 
	    		  article == null
	    		  ? string.Empty 
	    		  : article.HeadlineText ?? string.Empty
	    		  ;
        }

        private void ApplyFeed()
        {
            if (IsFeedIndexValid && ReaderTab != null)
            {
                string? feedName = ReaderTab.Header?.ToString() ?? string.Empty;
                ReaderFeedName.Content = feedName;

                if (FeedHeadlines != null && FeedHeadlines.Items != null && FeedHeadlines.Items.Count == 0)
                {
                    _FeedsArticles = FeedDataExchange.GetFeedArticles(
                        FeedConfiguration.SQLiteDbConnectionString,
                        feedName
                    );

                    if (_FeedsArticles != null)
                    {
                        ObservableCollection<FeedArticle>? indexedFeedArticles = new(_FeedsArticles.Values);
                        FeedHeadlines.ItemsSource = indexedFeedArticles;
                    }
                }
            }

            ApplyArticle(FeedHeadlines?.SelectedItem as FeedArticle);
        }

        private void UpdateFeedsOnInterval(object sender, EventArgs e)
        {
            _FeedUpdateTimer?.Stop();
            bool usingQuickInterval = _FeedUpdateTimer?.Interval == _QuickTimeSpan;

            if (usingQuickInterval)
            {
                _FeedUpdateTimer!.Interval = _MidTimeSpan;
            }

            _FeedUpdateTask.RunWorkerAsync();
        }

        private void FeedUpdateTask_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_FeedsInitialized)
            {
                _FeedUpdateTimer?.Stop();
                DownloadFeeds();
            }

            else
            {
                FeedDataExchange.RemoveExpiredArticlesFromDatabase(FeedConfiguration.SQLiteDbConnectionString);
                _Feeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);
            }
        }

        private void FeedUpdateTask_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_FeedsInitialized)
                {
                    ApplyNewFeeds();
                }

                else
                {
                    _FeedIndex = _Feeds != null && _Feeds.Count > 0 ? 0 : -1;
                    InitializeFeedConfigurations();
                    ApplyFeed();
                    _FeedsInitialized = true;
                    ReaderTabs.SelectionChanged += ReaderTabs_SelectionChanged;
                    ReaderManagerButton.IsEnabled = true;
                }
            });
            _FeedUpdateTimer?.Start();
        }

        private void InitializeFeedConfigurations()
        {
            if (_Feeds == null)
            {
                return;
            }

            foreach (string? feedName in _Feeds.Keys)
            {
                AddRSSTab(feedName);
            }

            if (ReaderTabs.Items.Count > 0)
            {
                ReaderTabs.SelectedIndex = 0;
            }
        }

        private void CheckRSSManagerUIUpdates(RSSManagerUI ui)
        {
            // Get the updated feeds from the manager
            ObservableCollection<BindableFeed>? ConfiguredFeeds = ui.Feeds;
            int ConfiguredFeedCount = ConfiguredFeeds.Count;
            // Update local feeds from database
            _Feeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);
            // Rebuild tabs based on current database state
            _ReaderTabItems.Clear();
            ReaderTabs.Items.Clear();
            // Reinitialize feed configurations
            _FeedIndex = _Feeds != null && _Feeds.Count > 0 ? 0 : -1;
            InitializeFeedConfigurations();

            if (ReaderTabs.Items.Count > 0)
            {
                ReaderTabs.SelectedIndex = 0;
            }

            ApplyFeed();
            UIRoot.UpdateLayout();
            ReaderTabs.UpdateLayout();
        }

        private void AddFeedToUIFollowingManagementUpdate(SortedList<string, Feed> activeFeeds, BindableFeed configuredFeed)
        {
            bool found = false;
            int activeFeedCount = activeFeeds.Count;
            string? configuredFeedName = configuredFeed.Name;
            bool hasExistingTabs = ReaderTabs.Items.Count > 0;

            for (int feedIndex = 0; feedIndex < activeFeedCount; feedIndex++)
            {
                string? feedName = activeFeeds.Keys[feedIndex];

                if (feedName == configuredFeedName)
                {
                    TabItem? foundTab = FindRSSFeedTab(feedName);
                    found = foundTab?.Header?.ToString() == feedName;

                    if (found)
                    {
                        break;
                    }
                }
            }

            if (!found)
            {
                AddRSSTab(configuredFeedName);
                TabItem? foundTab = FindRSSFeedTab(configuredFeedName);

                if (foundTab != null)
                {
                    ListBox? articlesUI = foundTab.Content as ListBox;

                    if (!hasExistingTabs && ReaderTabs.Items.Count > 0)
                    {
                        ReaderTabs.SelectedIndex = 0;
                    }

                    articlesUI?.UpdateLayout();
                }

                if (_Feeds != null && !_Feeds.ContainsKey(configuredFeedName))
                {
                    Feed? referenceFeed = BindableFeed.ConvertFeed(configuredFeed);
                    _Feeds.Add(configuredFeedName, referenceFeed);
                }
            }
        }

        private void UpdateFeedNamesFollowingManagementUpdate(SortedList<string, Feed> activeFeeds,
            BindableFeed configuredFeed)
        {
            if (_Feeds == null)
            {
                return;
            }

            int activeFeedCount = activeFeeds.Count;
            string? updatedFeedName = configuredFeed.Name;
            string? originalFeedName = configuredFeed.OriginalName;

            for (int feedIndex = 0; feedIndex < activeFeedCount; feedIndex++)
            {
                string? feedName = activeFeeds.Keys[feedIndex];

                if (originalFeedName == feedName && updatedFeedName != feedName)
                {
                    TabItem? foundTab = FindRSSFeedTab(feedName);

                    if (foundTab != null && foundTab.Header?.ToString() == feedName)
                    {
                        foundTab.Header = updatedFeedName;
                    }

                    if (activeFeeds.TryGetValue(feedName, out Feed? feedEntry))
                    {
                        feedEntry.FeedName = updatedFeedName;
                        activeFeeds.Remove(originalFeedName);
                        activeFeeds[updatedFeedName] = feedEntry;
                    }

                    break;
                }
            }
        }

        private void PruneFeedsFollowingManagementUpdate()
        {
            if (_Feeds == null)
            {
                return;
            }

            SortedList<string, Feed>? dbFeeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);
            List<string>? feedNames = new(_Feeds.Keys);

            foreach (string? feedName in feedNames)
            {
                if (!dbFeeds.ContainsKey(feedName))
                {
                    _Feeds.Remove(feedName);
                    TabItem? foundTab = FindRSSFeedTab(feedName);

                    if (foundTab != null && foundTab.Header?.ToString() == feedName)
                    {
                        _ReaderTabItems.Remove(foundTab);
                        ReaderTabs.Items.Remove(foundTab);
                    }
                }
            }
        }

        private void DownloadFeeds()
        {
            PruneExpiredArticles();
            SortedList<string, Feed>? DbFeeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);

            foreach (Feed? FeedEntry in DbFeeds.Values)
            {
                string? RSSXmlFilePath = RSSNetClient.DownloadFeed(FeedConfiguration.FeedSaveDirectoryPath, FeedEntry);

                if (File.Exists(RSSXmlFilePath))
                {
                    string? RSSIntegrationFilePath =
                        FeedFileUtil.GetRSSTabDelimitedFeedFilePath(FeedConfiguration.FeedSaveDirectoryPath, FeedEntry);
                    bool RSSXmlFileIsNewer = FeedFileUtil.CheckSourceFileNewer(RSSXmlFilePath, RSSIntegrationFilePath);

                    if (RSSXmlFileIsNewer)
                    {
                        List<FeedArticle>? Articles =
                            FeedFileConverter.TransformXmlFeedToFeedArticles(FeedConfiguration.FeedSaveDirectoryPath,
                                FeedEntry);
                        string? RSSTabDelimitedFilePath =
                            FeedFileConverter.WriteRSSArticlesToFile(FeedConfiguration.FeedSaveDirectoryPath, FeedEntry,
                                Articles);
                        bool RSSIntegrationPathIsValid = RSSIntegrationFilePath == RSSTabDelimitedFilePath;

                        if (RSSIntegrationPathIsValid && File.Exists(RSSTabDelimitedFilePath))
                        {
                            FeedDataExchange.ImportRSSFeedToDatabase(FeedConfiguration.FeedSaveDirectoryPath,
                                FeedConfiguration.FeedDbFilePath, FeedEntry);
                        }
                    }
                }
            }

            return;
        }

        private void ApplyNewFeeds()
        {
            if (_Feeds == null)
            {
                return;
            }

            List<string>? rssFeedNames = new(_Feeds.Keys);

            foreach (string? feedName in rssFeedNames)
            {
                SortedList<string, FeedArticle>? articles = FeedDataExchange.GetFeedArticles(
                    FeedConfiguration.SQLiteDbConnectionString,
                    feedName
                );
                List<string>? articleUrls = new(articles.Keys);
                TabItem? foundTab = FindRSSFeedTab(feedName);
                ListBox? articlesUI = foundTab?.Content as ListBox;

                if (articlesUI?.ItemsSource is ObservableCollection<FeedArticle> indexedFeedArticles)
                {
                    int addedArticleCount = 0;

                    foreach (string? articleUrl in articleUrls)
                    {
                        bool found = ContainsArticleUrl(indexedFeedArticles, articleUrl);

                        if (!found)
                        {
                            FeedArticle? article = articles[articleUrl];
                            indexedFeedArticles.Add(article);
                            addedArticleCount++;
                        }
                    }

                    if (addedArticleCount > 0)
                    {
                        articlesUI.ItemsSource = indexedFeedArticles;
                        articlesUI.UpdateLayout();
                    }
                }
            }
        }

        private static bool ContainsArticleUrl(IList<FeedArticle> articles, string articleUrl)
        {
            foreach (FeedArticle? article in articles)
            {
                if (string.Equals(article.ArticleUrl, articleUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
    }
}
