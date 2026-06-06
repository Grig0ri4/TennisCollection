using System;
using System.Data;
using System.Diagnostics; 
using System.Windows;
using System.Windows.Controls;

namespace TennisCatalog
{
    public partial class EncyclopediaPage : Page
    {
        private readonly DatabaseHelper _db;

        public EncyclopediaPage()
        {
            InitializeComponent();
            _db = new DatabaseHelper();
            LoadArticles();
        }

        private void LoadArticles()
        {
            try
            {
                DataTable dt = _db.GetEncyclopediaTitles();
                ArticlesListBox.ItemsSource = dt.DefaultView;

                if (ArticlesListBox.Items.Count > 0)
                {
                    ArticlesListBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ArticleContentBox.Text = "Ошибка загрузки списка статей: " + ex.Message;
            }
        }

        // Загрузка текста выбранной статьи
        private void ArticlesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArticlesListBox.SelectedValue != null)
            {
                int articleId = Convert.ToInt32(ArticlesListBox.SelectedValue);
                DataRowView selectedItem = (DataRowView)ArticlesListBox.SelectedItem;

                ArticleTitleHeader.Text = selectedItem["title"].ToString();

                try
                {
                    DataRow details = _db.GetArticleDetails(articleId);

                    if (details != null)
                    {
                        ArticleContentBox.Text = details["content"].ToString();

                        string url = details["external_url"] != DBNull.Value ? details["external_url"].ToString() : "";

                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            SourceLinkButton.Tag = url;
                            SourceLinkButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            SourceLinkButton.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ArticleContentBox.Text = "Не удалось загрузить содержимое: " + ex.Message;
                    SourceLinkButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SourceLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourceLinkButton.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    Process.Start(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось открыть ссылку: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}