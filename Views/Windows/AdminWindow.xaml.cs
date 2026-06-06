using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace TennisCatalog
{
    public partial class AdminWindow : Window
    {
        private readonly DatabaseHelper db;
        private readonly AdminFinalsManager finalsManager;
        private readonly AdminPlayersManager playersManager;
        private readonly MainWindow mainWindow;

        public AdminWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;

            db = new DatabaseHelper();

            finalsManager = new AdminFinalsManager(db, Msg);
            playersManager = new AdminPlayersManager(db, Msg);
            LoadTours();
        }

        private void LoadTours()
        {
            try
            {
                var dt = db.ExecuteQuery("SELECT name FROM voznesenskiy_ga.male_tours UNION SELECT name FROM voznesenskiy_ga.female_tours");
                AdminFinalsTourComboBox.ItemsSource = dt.DefaultView;
                AdminPlayersTourComboBox.ItemsSource = dt.DefaultView;
                AdminFinalsTourComboBox.DisplayMemberPath = AdminPlayersTourComboBox.DisplayMemberPath = "name";
            }
            catch (Exception ex)
            {
                Msg($"Ошибка загрузки туров: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private void AdminFinalsTourComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AdminFinalsTourComboBox.SelectedItem is DataRowView r)
            {
                finalsManager.SetTour(r["name"].ToString());
                finalsManager.LoadTournamentsAndPlayers(AdminFinalsTournamentComboBox, AdminFinalsPlayer1ComboBox, AdminFinalsPlayer2ComboBox, AdminFinalsWinnerComboBox);
                AdminFinalsGrid.ItemsSource = null;
            }
        }

        private void AdminFinalsYearTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AdminFinalsTournamentComboBox.SelectedValue != null)
                finalsManager.LoadFilteredFinals(AdminFinalsYearTextBox.Text, AdminFinalsTournamentComboBox, AdminFinalsGrid);
        }

        private void AdminFinalsTournamentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(AdminFinalsYearTextBox.Text))
                finalsManager.LoadFilteredFinals(AdminFinalsYearTextBox.Text, AdminFinalsTournamentComboBox, AdminFinalsGrid);
        }

        private void AdminPlayersTourComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AdminPlayersTourComboBox.SelectedItem is DataRowView r)
            {
                playersManager.SetTour(r["name"].ToString());
                playersManager.LoadAllPlayers(AdminPlayersGrid);
            }
        }

        private void AdminFinalsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            finalsManager.SetFinalId(AdminFinalsGrid.SelectedItem as DataRowView);
        }

        private void AdminPlayersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            playersManager.SetPlayerId(AdminPlayersGrid.SelectedItem as DataRowView);
        }

        private void AdminAddFinalButton_Click(object sender, RoutedEventArgs e)
        {
            finalsManager.AddFinal(AdminFinalsYearTextBox, AdminFinalsTournamentComboBox, AdminFinalsPlayer1ComboBox, AdminFinalsPlayer2ComboBox, AdminFinalsWinnerComboBox, AdminFinalsScoreTextBox, AdminFinalsGrid);
        }

        private void AdminDeleteFinalButton_Click(object sender, RoutedEventArgs e)
        {
            finalsManager.DeleteFinal(AdminFinalsYearTextBox, AdminFinalsTournamentComboBox, AdminFinalsGrid);
        }

        private void AdminAddPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            playersManager.AddPlayer(AdminPlayerFullNameTextBox, AdminPlayerCountryTextBox, AdminPlayersGrid);
        }

        private void AdminDeletePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            playersManager.DeletePlayer(AdminPlayerFullNameTextBox, AdminPlayerCountryTextBox, AdminPlayersGrid);
        }

        private void AdminExitButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Show();
            this.Close();
        }

        private void AdminPlayerFullNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            playersManager.FilterPlayersByName(AdminPlayerFullNameTextBox.Text, AdminPlayerCountryTextBox.Text, AdminPlayersGrid);
        }

        private void AdminPlayerCountryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            playersManager.FilterPlayersByName(AdminPlayerFullNameTextBox.Text, AdminPlayerCountryTextBox.Text, AdminPlayersGrid);
        }

        private void AdminPlayerComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.ItemsSource is DataView dv)
            {
                if (cb.IsKeyboardFocusWithin)
                {
                    string searchText = cb.Text.Replace("'", "''"); 
                    dv.RowFilter = string.IsNullOrWhiteSpace(searchText) ? "" : $"full_name LIKE '%{searchText}%'";
                    cb.IsDropDownOpen = true;
                }
            }
        }

        private void Msg(string s)
        {
            MessageBox.Show(s, "Админка", MessageBoxButton.OK,
                s.Contains("добавлен") || s.Contains("Удалён") ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
    }
}