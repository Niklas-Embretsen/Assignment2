using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Assignment2
{
    public partial class MainWindow : Window
    {

        public class Article
        {
            public string ArticleTitle { get; set; }
            public string WebsiteTitle { get; set; }
            public DateTime PubDate { get; set; }
        }

        public class Website
        {
            public string URL { get; set; }
            public string WebsiteTitle { get; set; }
        }

        private Thickness spacing = new Thickness(5);
        private HttpClient http = new HttpClient();
        // We will need these as instance variables to access in event handlers.
        private TextBox addFeedTextBox;
        private Button addFeedButton;
        private ComboBox selectFeedComboBox;
        private Button loadArticlesButton;
        private StackPanel articlePanel;

        public List<Website> websites = new List<Website>();

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            #region Window, Scroll, Grid
            #region Window
            // Window options
            Title = "Feed Reader";
            Width = 800;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            #endregion

            #region Scrolling
            // Scrolling
            var root = new ScrollViewer();
            root.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Content = root;
            #endregion

            #region Grid
            // Main grid
            var grid = new Grid();
            root.Content = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });//onödig?
            #endregion
            #endregion

            #region Add Feed
            #region Label
            var addFeedLabel = new Label
            {
                Content = "Feed URL:",
                Margin = spacing
            };
            grid.Children.Add(addFeedLabel);
            #endregion

            #region TextBox
            addFeedTextBox = new TextBox
            {
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedTextBox);
            Grid.SetColumn(addFeedTextBox, 1);
            #endregion

            #region Button
            addFeedButton = new Button
            {
                Content = "Add Feed",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(addFeedButton);
            Grid.SetColumn(addFeedButton, 2);
            addFeedButton.Click += FeedButton_Click;
            #endregion
            #endregion

            #region Select Feed
            #region Label
            var selectFeedLabel = new Label
            {
                Content = "Select Feed:",
                Margin = spacing
            };
            grid.Children.Add(selectFeedLabel);
            Grid.SetRow(selectFeedLabel, 1);
            #endregion

            #region ComboBox
            selectFeedComboBox = new ComboBox
            {
                Margin = spacing,
                Padding = spacing,
                IsEditable = false
            };
            grid.Children.Add(selectFeedComboBox);
            Grid.SetRow(selectFeedComboBox, 1);
            Grid.SetColumn(selectFeedComboBox, 1);
            selectFeedComboBox.Items.Add("All feeds");
            #endregion

            #region Button
            loadArticlesButton = new Button
            {
                Content = "Load Articles",
                Margin = spacing,
                Padding = spacing
            };
            grid.Children.Add(loadArticlesButton);
            Grid.SetRow(loadArticlesButton, 1);
            Grid.SetColumn(loadArticlesButton, 2);
            loadArticlesButton.Click += LoadArticles_Click;
            #endregion
            #endregion

            #region ArticlePanel
            articlePanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = spacing
            };
            grid.Children.Add(articlePanel);
            Grid.SetRow(articlePanel, 2);
            Grid.SetColumnSpan(articlePanel, 3);
            #endregion
        }

        private async Task<XDocument> LoadDocumentAsync(string url)
        {
            // This is just to simulate a slow/large data transfer and make testing easier.
            // Remove it if you want to.
            await Task.Delay(1000);
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();
            var feed = XDocument.Load(stream);
            return feed;
        }

        private async void FeedButton_Click(object sender, RoutedEventArgs e)
        {
            addFeedButton.IsEnabled = false;

            #region Read URL from "addFeedTextBox" and download "websiteTitle"
            string websiteURL = addFeedTextBox.Text;
            XDocument feedXDocument = await LoadDocumentAsync(websiteURL);
            string websiteTitle = GetWebsiteTitle_OfFeed(feedXDocument);
            #endregion

            if(!websites.Exists(website => website.URL == websiteURL))
            {
                #region Add the website if it's not already loaded

                #region Create "newWebsite"-object
                Website newWebsite = new Website { URL = websiteURL, WebsiteTitle = websiteTitle };
                #endregion

                #region Add "newWebsite" to both "List<Website> websites" and "selectFeedComboBox"
                //Since only the website-title is saved in "selectFeedComboBox", the URL of the website has to be stored separately in the list "websites".

                websites.Add(newWebsite);
                selectFeedComboBox.Items.Add(newWebsite.WebsiteTitle);
                #endregion

                #endregion
            }

            addFeedButton.IsEnabled = true;
        }

        private async void LoadArticles_Click(object sender, RoutedEventArgs e)
        {
            loadArticlesButton.IsEnabled = false;

            List<Task<XDocument>> XDocument_Tasks = new List<Task<XDocument>>();
            List<Article> articles = new List<Article>();

            string value_ComboBox = (string) selectFeedComboBox.SelectedItem;
            string URL_SelectedWebsite;

            if(value_ComboBox == "All feeds")
            {
                foreach (Website website in websites)
                {
                    XDocument_Tasks.Add(LoadDocumentAsync(website.URL));
                }
            }
            else
            {
                #region Get "URL_selectedWebsite" from "value_ComboBox"
                URL_SelectedWebsite = websites.First(website => website.WebsiteTitle == value_ComboBox).URL;
                #endregion

                #region Add a new "XDocument_Task" with the help of "URL_selectedWebsite" and place it in "XDocument_Tasks"
                XDocument_Tasks.Add(LoadDocumentAsync(URL_SelectedWebsite));
                #endregion
            }

            List<Task<XDocument>> XDocument_RemainingTasks = XDocument_Tasks.ToList();

            while(XDocument_RemainingTasks.Count() > 0)
            {
                Task<XDocument> XDocument_LoadedTask = await Task.WhenAny(XDocument_RemainingTasks);
                XDocument_RemainingTasks.Remove(XDocument_LoadedTask);
                XDocument XDocument_Loaded = await XDocument_LoadedTask;

                string websiteTitle = XDocument_Loaded.Descendants("title").First().Value;
                
                Article[] articles_Loaded = XDocument_Loaded
                    .Descendants("item")
                    .Take(5)
                    .Select(item => new Article() 
                    {
                        ArticleTitle = item.Descendants("title").First().Value,
                        WebsiteTitle = websiteTitle,
                        PubDate = DateTime.ParseExact(
                            item.Descendants("pubDate").First().Value.Substring(0, 25), 
                            "ddd, dd MMM yyyy HH:mm:ss", 
                            CultureInfo.InvariantCulture)
                    })
                    .ToArray();
                articles.AddRange(articles_Loaded);
            }

            articlePanel.Children.Clear();

            articles = articles.OrderByDescending(article => article.PubDate).ToList();
            foreach(Article article in articles)
            {
                var articleStackpanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = spacing
                };
                articlePanel.Children.Add(articleStackpanel);

                var articleTitle = new TextBlock
                {
                    Text = $"{article.PubDate.ToLocalTime()} - {article.ArticleTitle}",
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                articleStackpanel.Children.Add(articleTitle);

                var articleWebsite = new TextBlock
                {
                    Text = $"{article.WebsiteTitle}"
                };
                articleStackpanel.Children.Add(articleWebsite);
            }

            loadArticlesButton.IsEnabled = true;
        }

        private static string GetWebsiteTitle_OfFeed(XDocument document)
        {
            return document.Descendants("title").First().Value;
        }

    }
}
