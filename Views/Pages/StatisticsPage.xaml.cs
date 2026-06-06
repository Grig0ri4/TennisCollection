using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Npgsql;

namespace TennisCatalog
{
    public partial class StatisticsPage : Page, INotifyPropertyChanged
    {
        private int _currentIndex = 0;
        private string _mode;
        private int? _playerId, _playerId2;
        private string _playerName, _playerName2, _tour;

        private DatabaseHelper db = new DatabaseHelper();

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public StatisticsPage(string mode, int? p1Id, string p1Name, string tour, int? p2Id = null, string p2Name = null)
        {
            InitializeComponent();
            _mode = mode;
            _playerId = p1Id;
            _playerName = p1Name;
            _tour = tour;
            _playerId2 = p2Id;
            _playerName2 = p2Name;

            DataContext = this;

            if (_mode == "General")
            {
                StatsTypeCombo.SelectedIndex = 0;
            }

            UpdateData();
        }

        private void UpdateData()
        {
            if (_mode == "General")
            {
                BackButton.Visibility = Visibility.Collapsed;
                StatsTypeCombo.Visibility = Visibility.Visible;
                StatTitle.Text = "Глобальная аналитика";
                LoadGeneralStatsFromDb();
            }
            else if (_mode == "Compare")
            {
                StatTitle.Text = $"{_playerName} vs {_playerName2}";
                StatsTypeCombo.Visibility = Visibility.Collapsed;
                LoadComparisonData();
            }
            else
            {
                BackButton.Visibility = Visibility.Visible;
                StatsTypeCombo.Visibility = Visibility.Collapsed;
                StatTitle.Text = $"Аналитика: {_playerName}";
                LoadPlayerDataFromDb();
            }

            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
        }

        private void StatsTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mode == "General" && StatsTypeCombo != null)
            {
                _currentIndex = StatsTypeCombo.SelectedIndex;
                UpdateData();
            }
        }

        private void LoadGeneralStatsFromDb()
        {
            try
            {
                string query = "";
                string seriesName = "";
                SKColor color = SKColors.DodgerBlue;

                if (_currentIndex == 0) 
                {
                    query = @"
                        SELECT year, COUNT(*) as count 
                        FROM (
                            SELECT year FROM voznesenskiy_ga.male_finals
                            UNION ALL
                            SELECT year FROM voznesenskiy_ga.female_finals
                        ) combined
                        GROUP BY year
                        ORDER BY year";
                    seriesName = "Всего финалов";
                    color = SKColors.MediumPurple;
                }
                else 
                {
                    query = @"
                        SELECT type_name, COUNT(*) as count
                        FROM (
                            SELECT tt.name as type_name
                            FROM voznesenskiy_ga.male_finals f
                            JOIN voznesenskiy_ga.male_tournaments t ON f.tournament_id = t.id
                            JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                            UNION ALL
                            SELECT tt.name as type_name
                            FROM voznesenskiy_ga.female_finals f
                            JOIN voznesenskiy_ga.female_tournaments t ON f.tournament_id = t.id
                            JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                        ) all_finals
                        GROUP BY type_name";
                    seriesName = "Кол-во по категориям";
                    color = SKColors.Orange;
                }

                var dt = db.ExecuteQuery(query);
                var labels = dt.AsEnumerable().Select(r => r[0].ToString()).ToArray();
                var values = dt.AsEnumerable().Select(r => Convert.ToDouble(r[1])).ToArray();

                Series = new ISeries[] {
                    new ColumnSeries<double> {
                        Name = seriesName,
                        Values = values,
                        Fill = new SolidColorPaint(color)
                    }
                };
                XAxes = new Axis[] { new Axis { Labels = labels } };
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void LoadPlayerDataFromDb()
        {
            try
            {
                string prefix = _tour == "ATP" ? "male" : "female";
                string query = $@"
                    SELECT tt.name, count(*) as wins 
                    FROM voznesenskiy_ga.{prefix}_finals f
                    JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                    JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                    WHERE f.winner_id = @pId
                    GROUP BY tt.name";

                var dt = db.ExecuteQuery(query, new[] { new NpgsqlParameter("@pId", _playerId.Value) });
                var labels = dt.AsEnumerable().Select(r => r[0].ToString()).ToArray();
                var values = dt.AsEnumerable().Select(r => Convert.ToDouble(r[1])).ToArray();

                Series = new ISeries[] {
                    new ColumnSeries<double> {
                        Name = "Титулы",
                        Values = values,
                        Fill = new SolidColorPaint(SKColors.SeaGreen)
                    }
                };
                XAxes = new Axis[] { new Axis { Labels = labels } };
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void LoadComparisonData()
        {
            try
            {
                string prefix = _tour == "ATP" ? "male" : "female";
                var data1 = GetPlayerStatsMap(_playerId.Value, prefix);
                var data2 = GetPlayerStatsMap(_playerId2.Value, prefix);
                var allTypes = data1.Keys.Union(data2.Keys).ToList();

                var v1 = allTypes.Select(t => data1.ContainsKey(t) ? (double)data1[t] : 0).ToArray();
                var v2 = allTypes.Select(t => data2.ContainsKey(t) ? (double)data2[t] : 0).ToArray();

                Series = new ISeries[] {
                    new ColumnSeries<double> { Name = _playerName, Values = v1, Fill = new SolidColorPaint(SKColors.DodgerBlue) },
                    new ColumnSeries<double> { Name = _playerName2, Values = v2, Fill = new SolidColorPaint(SKColors.OrangeRed) }
                };
                XAxes = new Axis[] { new Axis { Labels = allTypes.ToArray() } };
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private Dictionary<string, int> GetPlayerStatsMap(int id, string prefix)
        {
            string query = $@"
                SELECT tt.name, count(*) 
                FROM voznesenskiy_ga.{prefix}_finals f
                JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                WHERE f.winner_id = @id GROUP BY tt.name";
            var dt = db.ExecuteQuery(query, new[] { new NpgsqlParameter("@id", id) });
            return dt.AsEnumerable().ToDictionary(r => r[0].ToString(), r => Convert.ToInt32(r[1]));
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Back_Click(object sender, RoutedEventArgs e) => NavigationService.GoBack();
    }
}