using System.Collections.ObjectModel;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using gautier.rss.data;
using gautier.rss.ui.UIData;

namespace gautier.rss.ui
{
    public partial class RSSManagerUI : Window
    {
        private SortedList<string, Feed> _OriginalFeeds = new();
        private ObservableCollection<BindableFeed> _Feeds = new();

        public ObservableCollection<BindableFeed> Feeds
        {
            get => _Feeds;
        }

        private BindableFeed CurrentFeed
        {
            get => FeedsGrid.SelectedItem as BindableFeed;
        }

        public RSSManagerUI()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            FeedDataExchange.RemoveExpiredArticlesFromDatabase(FeedConfiguration.SQLiteDbConnectionString);
            _OriginalFeeds = FeedDataExchange.GetAllFeeds(FeedConfiguration.SQLiteDbConnectionString);
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (RetrieveLimitHrs != null)
            {
                RetrieveLimitHrs.PropertyChanged += RetrieveLimitHrs_PropertyChanged;
            }

            if (RetentionDays != null)
            {
                RetentionDays.PropertyChanged += RetentionDays_PropertyChanged;
            }

            ResetInput();
            FeedName.Focus();

            if (DeleteButton != null)
            {
                DeleteButton.Click += DeleteButton_Click;
            }

            if (SaveButton != null)
            {
                SaveButton.Click += SaveButton_Click;
            }

            if (NewButton != null)
            {
                NewButton.Click += NewButton_Click;
            }

            LayoutFeedsGrid();

            if (FeedsGrid != null)
            {
                FeedsGrid.SelectionChanged += FeedsGrid_SelectionChanged;
            }

            if (_Feeds.Count > 0)
            {
                FeedsGrid.SelectedIndex = 0;
            }

            Loaded -= OnWindowLoaded;
        }

        private void RetrieveLimitHrs_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty && RetrieveLimitHrsValue != null)
            {
                int value = Convert.ToInt32(RetrieveLimitHrs.Value);
                RetrieveLimitHrsValue.Text = value.ToString();
            }
        }

        private void RetentionDays_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty && RetentionDaysValue != null)
            {
                int value = Convert.ToInt32(RetentionDays.Value);
                RetentionDaysValue.Text = value.ToString();
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            FeedsGrid.SelectedIndex = -1;
            ResetInput();
            FeedName.Focus();
        }

        private BindableFeed ResetInput()
        {
            BindableFeed? BFeed = new();

            if (FeedName != null)
            {
                FeedName.Text = BFeed.Name;
            }

            if (FeedUrl != null)
            {
                FeedUrl.Text = BFeed.Url;
            }

            if (RetrieveLimitHrs != null)
            {
                RetrieveLimitHrs.Value = BFeed.RetrieveLimitHrs;
            }

            if (RetentionDays != null)
            {
                RetentionDays.Value = BFeed.RetentionDays;
            }

            UpdateValueDisplays();
            return BFeed;
        }

        private void UpdateValueDisplays()
        {
            if (RetrieveLimitHrsValue != null && RetrieveLimitHrs != null)
            {
                RetrieveLimitHrsValue.Text = RetrieveLimitHrs.Value.ToString("0");
            }

            if (RetentionDaysValue != null && RetentionDays != null)
            {
                RetentionDaysValue.Text = RetentionDays.Value.ToString("0");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            BindableFeed? CFeed = CurrentFeed ?? new BindableFeed();
            bool InputIsValid = CheckInput(CFeed);

            if (!InputIsValid)
            {
                return;
            }

            CFeed.Name = FeedName.Text;
            CFeed.Url = FeedUrl.Text;
            CFeed.RetrieveLimitHrs = Convert.ToInt32(RetrieveLimitHrs.Value);
            CFeed.RetentionDays = Convert.ToInt32(RetentionDays.Value);

            // Set the date/time back a few days to trigger download of the feed
            // when it is newly added.
            if (CFeed.Id < 1)
            {
                CFeed.LastRetrieved = DateTime.Today.AddDays(-4);
            }

            Feed? DbInputFeed = BindableFeed.ConvertFeed(CFeed);
            Feed? UpdatedFeed = FeedDataExchange.UpdateFeedConfigurationInDatabase(
                FeedConfiguration.FeedDbFilePath,
                DbInputFeed
            );
            bool InputAndUpdatedFeedsMatch = DbInputFeed.FeedName == UpdatedFeed.FeedName;

            if (InputAndUpdatedFeedsMatch)
            {
                bool Found = false;

                foreach (BindableFeed? FeedEntry in _Feeds)
                {
                    Found = UpdatedFeed.FeedName == FeedEntry.Name;

                    if (Found)
                    {
                        break;
                    }
                }

                if (!Found)
                {
                    BindableFeed? FeedOutput = BindableFeed.ConvertFeedNarrow(UpdatedFeed);
                    _Feeds.Add(FeedOutput);
                    FeedsGrid.SelectedItem = FeedOutput;
                }

                else
                {
                    foreach (BindableFeed? FeedEntry in _Feeds)
                    {
                        if (FeedEntry.Name == UpdatedFeed.FeedName)
                        {
                            FeedEntry.Id = UpdatedFeed.DbId;
                            FeedEntry.Url = UpdatedFeed.FeedUrl;
                            FeedEntry.RetrieveLimitHrs = int.Parse(UpdatedFeed.RetrieveLimitHrs);
                            FeedEntry.RetentionDays = int.Parse(UpdatedFeed.RetentionDays);
                            FeedEntry.LastRetrieved = DateTime.Parse(UpdatedFeed.LastRetrieved);
                            break;
                        }
                    }
                }
            }
        }

        private bool CheckInput(BindableFeed feed)
        {
            int ErrorCount = 0;
            StringBuilder? Errors = new();
            Action<string>? DispatchError = (string errorMessage) =>
            {
                Errors.AppendLine(errorMessage);
                ErrorCount++;
            };
            string? FeedNameText = FeedName.Text;
            string? FeedUrlText = FeedUrl.Text;

            if (string.IsNullOrWhiteSpace(FeedNameText))
            {
                DispatchError("Feed Name cannot be blank.");
            }

            if (string.IsNullOrWhiteSpace(FeedUrlText))
            {
                DispatchError("Feed URL cannot be blank.");
            }

            // Unique Name Validation
            SortedList<string, int>? Names = new();

            foreach (BindableFeed? FeedEntry in _Feeds)
            {
                string? EntryFeedName = FeedEntry.Name;

                if (Names.ContainsKey(EntryFeedName))
                {
                    int RowCount = Names[EntryFeedName];
                    RowCount++;
                    Names[EntryFeedName] = RowCount;
                }

                else
                {
                    if (feed.Id != FeedEntry.Id)
                    {
                        Names[EntryFeedName] = 1;
                    }
                }
            }

            if (Names.ContainsKey(FeedNameText))
            {
                DispatchError("Feed Name must be unique.");
            }

            // Unique URL Validation
            SortedList<string, int>? Urls = new();

            foreach (BindableFeed? FeedEntry in _Feeds)
            {
                string? EntryUrl = FeedEntry.Url;

                if (Urls.ContainsKey(EntryUrl))
                {
                    int RowCount = Urls[EntryUrl];
                    RowCount++;
                    Urls[EntryUrl] = RowCount;
                }

                else
                {
                    if (feed.Id != FeedEntry.Id)
                    {
                        Urls[EntryUrl] = 1;
                    }
                }
            }

            if (Urls.ContainsKey(FeedUrlText))
            {
                DispatchError("Feed URL must be unique.");
            }

            // URL Format Validation
            bool IsValidUrl = !string.IsNullOrWhiteSpace(FeedUrlText) &&
                (FeedUrlText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    FeedUrlText.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

            if (!IsValidUrl)
            {
                DispatchError("Feed URL must be in the proper format starting with http:// or https://.");
            }

            bool IsValid = ErrorCount == 0;

            if (!IsValid)
            {
                Window? messageBox = new()
                {
                    Title = "Validation Error",
                    Content = new TextBlock { Text = Errors.ToString() },
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                messageBox.ShowDialog(this);
            }

            return IsValid;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BindableFeed? CFeed = CurrentFeed;

            if (CFeed != null && CFeed.Id > 0)
            {
                bool IsDeleted = FeedDataExchange.RemoveFeedFromDatabase(
                    FeedConfiguration.FeedDbFilePath,
                    CFeed.Id
                );

                if (IsDeleted)
                {
                    _Feeds.Remove(CFeed);

                    if (_Feeds.Count > 0)
                    {
                        FeedsGrid.SelectedIndex = 0;
                    }

                    else
                    {
                        ResetInput();
                    }
                }
            }
        }

        private void LayoutFeedsGrid()
        {
            _Feeds = BindableFeed.ConvertFeeds(_OriginalFeeds);

            // DEBUG: Check what we're binding
            Console.WriteLine($"DEBUG: Binding {_Feeds.Count} feeds to DataGrid");

            foreach (BindableFeed? feed in _Feeds)
            {
                Console.WriteLine($"  - {feed.Name}: {feed.Url}");
            }

            // DEBUG: Check if DataGrid exists
            if (FeedsGrid == null)
            {
                Console.WriteLine("ERROR: FeedsGrid is null!");
                return;
            }

            Console.WriteLine($"DEBUG: FeedsGrid found. Setting ItemsSource...");

            FeedsGrid.ItemsSource = _Feeds;

            // DEBUG: Force refresh
            FeedsGrid.InvalidateVisual();
            FeedsGrid.InvalidateMeasure();
            FeedsGrid.InvalidateArrange();

            //Console.WriteLine($"DEBUG: DataGrid should now show {FeedsGrid.ItemsSource.Count()} items");
        }

        private void FeedsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ResetInput has side-effects. It clears the input fields and returns a default instance.
            // Handles cases where changing selected index is not recognized by the Grid.
            // Such a case can be when there are no rows yet.
            BindableFeed? BFeed = CurrentFeed ?? ResetInput();

            if (FeedName != null)
            {
                FeedName.Text = BFeed.Name;
            }

            if (FeedUrl != null)
            {
                FeedUrl.Text = BFeed.Url;
            }

            if (RetrieveLimitHrs != null)
            {
                RetrieveLimitHrs.Value = BFeed.RetrieveLimitHrs;
            }

            if (RetentionDays != null)
            {
                RetentionDays.Value = BFeed.RetentionDays;
            }

            // Update value displays
            UpdateValueDisplays();
            FeedName.Focus();
        }
    }
}
