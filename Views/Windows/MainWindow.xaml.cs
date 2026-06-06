using System;
using System.Windows;

namespace TennisCatalog
{
    public partial class MainWindow : Window
    {
        public string CurrentUser { get; set; }
        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            LoadPages();
        }

        // Загружка всех страниц-вкладок в соответствующие фреймы при старте
        private void LoadPages()
        {
            try
            {
                if (FinalsFrame != null) FinalsFrame.Navigate(new FinalsPage());
                if (PlayersFrame != null) PlayersFrame.Navigate(new PlayersPage());

                if (StatsFrame != null) StatsFrame.Navigate(new StatisticsPage("General", null, null, null));

                if (EncyclopediaFrame != null) EncyclopediaFrame.Navigate(new EncyclopediaPage());
                if (UserFrame != null) UserFrame.Navigate(new UserPage(CurrentUser));
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        public void SetCurrentUser(string username)
        {
            CurrentUser = username;
            if (UserFrame != null) UserFrame.Navigate(new UserPage(CurrentUser));
            AdminPanelButton.Visibility = (username == "admin") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MyDiaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = new DatabaseHelper();
                var diaryWindow = new DiaryWindow(db, Models.UserSession.CurrentUserId);
                diaryWindow.Owner = this;
                diaryWindow.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == "admin")
            {
                var adminWindow = new AdminWindow(this);
                adminWindow.Show();
                this.Hide();
            }
        }
    }
}