using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Win32;
using Npgsql;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;

namespace TennisCatalog
{
    public class PlayerPageManager
    {
        private readonly DatabaseHelper db;
        private readonly Action<string> showMessage;
        private string selectedTour;
        private DataView originalPlayersView;

        public PlayerPageManager(DatabaseHelper db, Action<string> showMessage)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        public void LoadTours(ComboBox tourComboBox)
        {
            if (tourComboBox == null) return;
            try
            {
                string query = "SELECT name FROM voznesenskiy_ga.male_tours UNION SELECT name FROM voznesenskiy_ga.female_tours";
                var dt = db.ExecuteQuery(query);
                if (dt == null || dt.Rows.Count == 0) return;
                tourComboBox.DisplayMemberPath = "name";
                tourComboBox.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка туров: {ex.Message}"); }
        }

        public void TourComboBox_SelectionChanged(ComboBox tourComboBox, Action<string> loadPlayersCallback)
        {
            if (tourComboBox?.SelectedItem == null) return;
            selectedTour = (tourComboBox.SelectedItem as DataRowView)?["name"]?.ToString();
            if (!string.IsNullOrEmpty(selectedTour))
                loadPlayersCallback?.Invoke(selectedTour);
        }

        public void LoadPlayers(string tour, ListBox playersList)
        {
            if (playersList == null || string.IsNullOrEmpty(tour)) return;
            try
            {
                string prefix = tour.ToUpper() == "ATP" ? "male" : "female";
                string query = $@"SELECT DISTINCT p.id, p.full_name, p.country
                                  FROM voznesenskiy_ga.{prefix}_players p
                                  JOIN voznesenskiy_ga.{prefix}_finals f ON p.id = f.winner_id
                                  ORDER BY p.full_name";
                var dt = db.ExecuteQuery(query);
                if (dt != null)
                {
                    originalPlayersView = dt.DefaultView;
                    playersList.ItemsSource = originalPlayersView;
                    playersList.DisplayMemberPath = "full_name";
                    playersList.SelectedValuePath = "id";
                }
            }
            catch (Exception ex) { showMessage($"Ошибка игроков: {ex.Message}"); }
        }

        public void SearchTextBox_TextChanged(string filter, ListBox playersList)
        {
            if (originalPlayersView != null)
                originalPlayersView.RowFilter = string.IsNullOrEmpty(filter) ? "" : $"full_name LIKE '%{filter}%' OR country LIKE '%{filter}%'";
        }

        public void PlayersList_SelectionChanged(ListBox playersList, Action<DataRowView> loadStatsCallback)
        {
            if (playersList?.SelectedItem is DataRowView row) loadStatsCallback?.Invoke(row);
        }

        // Загрузка личной статистики игрока по типам турниров для построения графиков
        public void LoadStats(string tour, int playerId, string fullName, string country, TextBlock playerNameText, TextBlock totalTournamentsText, ItemsControl tournamentStats, PieChart pieChart)
        {
            try
            {
                if (string.IsNullOrEmpty(tour)) return;

                string prefix = tour.ToUpper() == "ATP" ? "male" : "female";
                var typesDt = db.ExecuteQuery("SELECT id, name FROM voznesenskiy_ga.tournament_types");
                if (typesDt == null) return;

                int total = 0;
                var statsList = new List<dynamic>();
                var chartSeries = new List<ISeries>();

                foreach (DataRow typeRow in typesDt.Rows)
                {
                    int typeId = Convert.ToInt32(typeRow["id"]);
                    string typeName = typeRow["name"].ToString();

                    string countQuery = $@"SELECT COUNT(*) FROM voznesenskiy_ga.{prefix}_finals f 
                                           JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                                           WHERE f.winner_id = @pId AND t.type_id = @tId";

                    int count = Convert.ToInt32(db.ExecuteScalar(countQuery, new[] {
                        new NpgsqlParameter("@pId", playerId),
                        new NpgsqlParameter("@tId", typeId)
                    }));
                    total += count;

                    string details = "";
                    if (count > 0)
                    {
                        var dtDetails = db.ExecuteQuery($@"SELECT t.name, f.year FROM voznesenskiy_ga.{prefix}_finals f 
                                                           JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                                                           WHERE f.winner_id = @pId AND t.type_id = @tId ORDER BY f.year DESC",
                                                           new[] { new NpgsqlParameter("@pId", playerId), new NpgsqlParameter("@tId", typeId) });

                        details = string.Join(", ", dtDetails.AsEnumerable().Select(r => $"{r["name"]} ({r["year"]})"));

                        if (pieChart != null)
                            chartSeries.Add(new PieSeries<int> { Values = new[] { count }, Name = typeName });
                    }
                    statsList.Add(new { TypeName = typeName, Count = count, Details = details });
                }

                if (playerNameText != null) playerNameText.Text = $"{fullName} ({country})";
                if (totalTournamentsText != null) totalTournamentsText.Text = total.ToString();
                if (tournamentStats != null) tournamentStats.ItemsSource = statsList;

                if (pieChart != null)
                {
                    pieChart.Series = chartSeries;
                }
            }
            catch (Exception ex) { showMessage($"Ошибка статистики: {ex.Message}"); }
        }

        // Экспорт персональной статистики в Excel
        public void ExportButton_Click(ItemsControl tournamentStats, TextBlock playerNameText, TextBlock totalTournamentsText)
        {
            if (tournamentStats?.ItemsSource == null) { showMessage("Нет данных!"); return; }
            var items = tournamentStats.ItemsSource as IEnumerable<dynamic>;

            var dialog = new SaveFileDialog { FileName = $"Отчет_{playerNameText?.Text}", DefaultExt = ".xlsx", Filter = "Excel |*.xlsx" };
            if (dialog.ShowDialog() != true) return;

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Статистика");
                ws.Cell("A1").Value = "ИНДИВИДУАЛЬНЫЙ ОТЧЕТ ИГРОКА";
                ws.Cell("A2").Value = playerNameText?.Text;
                ws.Cell("A3").Value = $"Всего титулов: {totalTournamentsText?.Text}";

                var tableHeader = ws.Range("A6:C6");
                ws.Cell("A6").Value = "Тип турнира";
                ws.Cell("B6").Value = "Количество";
                ws.Cell("C6").Value = "Список побед";
                tableHeader.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

                int row = 7;
                foreach (var item in items)
                {
                    ws.Cell(row, 1).Value = item.TypeName;
                    ws.Cell(row, 2).Value = item.Count;
                    ws.Cell(row, 3).Value = item.Details;
                    row++;
                }
                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                showMessage("Отчет успешно сохранен!");
            }
        }

        public void ClearButton_Click(ComboBox tourComboBox, ListBox playersList, TextBox searchTextBox, StackPanel statsPanel, ItemsControl tournamentStats)
        {
            if (tourComboBox != null) tourComboBox.SelectedItem = null;
            if (playersList != null) playersList.ItemsSource = null;
            if (searchTextBox != null) searchTextBox.Text = "";
            if (tournamentStats != null) tournamentStats.ItemsSource = null;
            if (statsPanel != null) statsPanel.Visibility = System.Windows.Visibility.Collapsed;
            selectedTour = null;
        }
    }
}