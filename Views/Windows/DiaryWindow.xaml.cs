using System;
using System.Windows;
using System.Windows.Controls;
using TennisCatalog.Models;

namespace TennisCatalog
{
    public partial class DiaryWindow : Window
    {
        private readonly DatabaseHelper _db;
        private readonly int _userId;

        public DiaryWindow(DatabaseHelper db, int userId)
        {
            InitializeComponent();
            _db = db;
            _userId = userId;
            LoadDiaryData();
        }

        private void LoadDiaryData()
        {
            try
            {
                var matches = _db.GetUserDiary(_userId);
                DiaryCardsControl.ItemsSource = matches;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке: " + ex.Message);
            }
        }

        private void RemoveMatch_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is int id)
            {
                if (MessageBox.Show("Удалить этот матч из дневника?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _db.RemoveFromDiary(id);
                    LoadDiaryData(); 
                }
            }
        }

        private void EditNotes_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var match = btn?.DataContext as DiaryMatch;

            if (match != null)
            {
                var editWin = new DiaryEditWindow(match.Notes, match.Rating);
                editWin.Owner = this;

                if (editWin.ShowDialog() == true)
                {
                    _db.UpdateDiaryEntry(match.DiaryId, editWin.Notes, editWin.Rating);

                    LoadDiaryData();
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}