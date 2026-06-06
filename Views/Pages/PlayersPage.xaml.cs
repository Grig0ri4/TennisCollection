using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Data;

namespace TennisCatalog
{
    public partial class PlayersPage : Page
    {
        private readonly PlayerPageManager playerPageManager;

        public PlayersPage()
        {
            InitializeComponent();
            DatabaseHelper db = new DatabaseHelper("Host=245e1-rw.db.pub.dbaas.postgrespro.ru;Port=5432;Database=dbdiploma;Username=voznesenskiy_ga;Password=83WM%K16#hy;Trust Server Certificate=true;");
            playerPageManager = new PlayerPageManager(db, msg => MessageBox.Show(msg));
            playerPageManager.LoadTours(TourComboBox);
        }

        private void TourComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            playerPageManager?.TourComboBox_SelectionChanged(TourComboBox, tour => playerPageManager.LoadPlayers(tour, PlayersList));

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
            playerPageManager?.SearchTextBox_TextChanged(SearchTextBox.Text, PlayersList);

        private void PlayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlayersList.SelectedItem is DataRowView row)
            {
                StatsPanel.Visibility = Visibility.Visible;
                string tour = (TourComboBox.SelectedItem as DataRowView)?["name"]?.ToString() ?? "ATP";
                playerPageManager.LoadStats(tour, (int)row["id"], row["full_name"].ToString(), row["country"].ToString(),
                    PlayerNameText, TotalTournamentsText, TournamentStats, null);
            }
        }

        private void OpenFullStats_Click(object sender, RoutedEventArgs e)
        {
            if (PlayersList.SelectedItem is DataRowView row)
            {
                string tour = (TourComboBox.SelectedItem as DataRowView)?["name"]?.ToString();
                NavigationService.Navigate(new StatisticsPage("Player", (int)row["id"], row["full_name"].ToString(), tour));
            }
        }

        private void OpenCompareWindow_Click(object sender, RoutedEventArgs e)
        {
            string tour = (TourComboBox.SelectedItem as DataRowView)?["name"]?.ToString() ?? "ATP";
            CompareWindow win = new CompareWindow(tour);
            win.Owner = Window.GetWindow(this);
            win.Show();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) =>
            playerPageManager.ExportButton_Click(TournamentStats, PlayerNameText, TotalTournamentsText);

        private void ClearButton_Click(object sender, RoutedEventArgs e) =>
            playerPageManager.ClearButton_Click(TourComboBox, PlayersList, SearchTextBox, StatsPanel, TournamentStats);

        private void ExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}