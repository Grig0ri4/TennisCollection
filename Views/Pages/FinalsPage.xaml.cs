using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Data;
using TennisCatalog.Models;
using ClosedXML.Excel;

namespace TennisCatalog
{
    public partial class FinalsPage : Page
    {
        private readonly FinalsViewManager finalsViewManager;
        private readonly DatabaseHelper databaseHelper;

        public FinalsPage()
        {
            InitializeComponent();

            databaseHelper = new DatabaseHelper();

            finalsViewManager = new FinalsViewManager(
                databaseHelper,
                message => MessageBox.Show(message)
            );

            YearComboBox.IsEnabled = TypeComboBox.IsEnabled = TournamentComboBox.IsEnabled = false;
            finalsViewManager.LoadTours(TourComboBox);
            finalsViewManager.LoadTypes(TypeComboBox);
            finalsViewManager.LoadTotalFinals(TotalFinalsText);
        }

        private void GeneralStats_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StatisticsPage("General", null, null, null));
        }

        // Метод добавления выбранного матча в дневник пользователя
        private void AddToDiary_Click(object sender, RoutedEventArgs e)
        {
            var selectedRow = FinalsGrid.SelectedItem as DataRowView;
            if (selectedRow == null) { MessageBox.Show("Пожалуйста, выберите матч!"); return; }

            try
            {
                int finalId = Convert.ToInt32(selectedRow["id"]);
                string tour = TourComboBox.Text.ToLower();
                bool success = databaseHelper.AddToDiary(UserSession.CurrentUserId, finalId, tour);
                if (success) MessageBox.Show("Матч добавлен в ваш дневник!");
                else MessageBox.Show("Этот матч уже есть в вашем дневнике.");
            }
            catch (Exception ex) { MessageBox.Show("Ошибка добавления: " + ex.Message); }
        }

        private void TourComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            finalsViewManager.TourComboBox_SelectionChanged(TourComboBox, YearComboBox, TypeComboBox, TournamentComboBox, () => { BackButton.IsEnabled = true; });

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            finalsViewManager.YearComboBox_SelectionChanged(YearComboBox, TypeComboBox, TournamentComboBox, () => { });

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            finalsViewManager.TypeComboBox_SelectionChanged(TypeComboBox, TournamentComboBox, () => { });

        private void TournamentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            finalsViewManager.TournamentComboBox_SelectionChanged(TournamentComboBox, FinalsGrid, () => { finalsViewManager.LoadFinalDetails(FinalsGrid); });

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            finalsViewManager.BackButton_Click(TourComboBox, YearComboBox, TypeComboBox, TournamentComboBox, FinalsGrid, () => { BackButton.IsEnabled = finalsViewManager.CurrentStep > 0; });

        private void ClearButton_Click(object sender, RoutedEventArgs e) =>
            finalsViewManager.ClearButton_Click(TourComboBox, YearComboBox, TypeComboBox, TournamentComboBox, FinalsGrid, () => { BackButton.IsEnabled = false; });

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Window.GetWindow(this)?.Close();
        }
    }
}