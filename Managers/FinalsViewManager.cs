using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Npgsql;

namespace TennisCatalog
{
    public class FinalsViewManager
    {
        private readonly DatabaseHelper db;
        private readonly Action<string> showMessage;
        private string tourF;
        private int? selectedYear;
        private int? selectedTypeId;
        private int? selectedTournamentId;
        private int currentStep = 0;

        public int CurrentStep => currentStep;

        public FinalsViewManager(DatabaseHelper db, Action<string> showMessage)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        public void LoadTours(ComboBox tourComboBox)
        {
            try
            {
                var dt = db.ExecuteQuery("SELECT name FROM voznesenskiy_ga.male_tours UNION SELECT name FROM voznesenskiy_ga.female_tours");
                if (dt.Rows.Count == 0) return;
                tourComboBox.DisplayMemberPath = "name";
                tourComboBox.SelectedValuePath = "name";
                tourComboBox.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки туров: {ex.Message}"); }
        }

        public void LoadTypes(ComboBox typeComboBox)
        {
            try
            {
                var dt = db.ExecuteQuery("SELECT id, name FROM voznesenskiy_ga.tournament_types ORDER BY name");
                if (dt.Rows.Count == 0) return;
                typeComboBox.DisplayMemberPath = "name";
                typeComboBox.SelectedValuePath = "id";
                typeComboBox.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки типов: {ex.Message}"); }
        }

        public void LoadTotalFinals(TextBlock totalFinalsText)
        {
            try
            {
                var dt = db.ExecuteQuery("SELECT voznesenskiy_ga.get_total_finals() AS total");
                int total = dt.Rows.Count > 0 && dt.Rows[0]["total"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["total"]) : 0;
                totalFinalsText.Text = $"Всего финалов: {total}";
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки общего количества: {ex.Message}"); }
        }

        public void TourComboBox_SelectionChanged(ComboBox tourComboBox, ComboBox yearComboBox, ComboBox typeComboBox, ComboBox tournamentComboBox, Action enableNextStep)
        {
            if (tourComboBox.SelectedValue == null) return;
            tourF = tourComboBox.SelectedValue.ToString();
            currentStep = 1; 

            selectedYear = null; selectedTypeId = null; selectedTournamentId = null;

            tourComboBox.IsEnabled = false;
            yearComboBox.IsEnabled = true;
            typeComboBox.IsEnabled = false;
            tournamentComboBox.IsEnabled = false;

            yearComboBox.ItemsSource = null;
            typeComboBox.SelectedItem = null;
            tournamentComboBox.ItemsSource = null;

            enableNextStep?.Invoke();
            LoadYears(yearComboBox);
        }

        public void YearComboBox_SelectionChanged(ComboBox yearComboBox, ComboBox typeComboBox, ComboBox tournamentComboBox, Action enableNextStep)
        {
            if (yearComboBox.SelectedValue == null) return;
            selectedYear = int.Parse(yearComboBox.SelectedValue.ToString());
            currentStep = 2; 

            selectedTypeId = null; selectedTournamentId = null;

            yearComboBox.IsEnabled = false;
            typeComboBox.IsEnabled = true;
            tournamentComboBox.IsEnabled = false;

            typeComboBox.SelectedItem = null;
            tournamentComboBox.ItemsSource = null;

            enableNextStep?.Invoke();
        }

        public void TypeComboBox_SelectionChanged(ComboBox typeComboBox, ComboBox tournamentComboBox, Action enableNextStep)
        {
            if (typeComboBox.SelectedValue == null) return;
            selectedTypeId = (int)typeComboBox.SelectedValue;
            currentStep = 3; 

            selectedTournamentId = null;

            typeComboBox.IsEnabled = false;
            tournamentComboBox.IsEnabled = true;

            tournamentComboBox.ItemsSource = null;

            enableNextStep?.Invoke();
            LoadTournaments(tournamentComboBox);
        }

        public void TournamentComboBox_SelectionChanged(ComboBox tournamentComboBox, DataGrid finalsGrid, Action loadFinalDetails)
        {
            if (tournamentComboBox.SelectedValue == null) return;
            selectedTournamentId = (int)tournamentComboBox.SelectedValue;
            currentStep = 4;

            tournamentComboBox.IsEnabled = false; 

            loadFinalDetails?.Invoke();
        }

        public void LoadFinalDetails(DataGrid finalsGrid)
        {
            try
            {
                if (string.IsNullOrEmpty(tourF) || !selectedYear.HasValue || !selectedTypeId.HasValue || !selectedTournamentId.HasValue) return;

                string prefix = tourF == "ATP" ? "male" : "female";
                string sql = $@"
                    SELECT
                        f.id,
                        p1.full_name AS player1,
                        p2.full_name AS player2,
                        pw.full_name AS winner,
                        f.score
                    FROM voznesenskiy_ga.{prefix}_finals f
                    JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                    JOIN voznesenskiy_ga.{prefix}_players p1 ON f.player1_id = p1.id
                    JOIN voznesenskiy_ga.{prefix}_players p2 ON f.player2_id = p2.id
                    JOIN voznesenskiy_ga.{prefix}_players pw ON f.winner_id = pw.id
                    WHERE f.year = @year AND f.tournament_id = @tournament_id AND t.type_id = @type_id";

                var dt = db.ExecuteQuery(sql, new[] {
                    new NpgsqlParameter("@year", selectedYear.Value),
                    new NpgsqlParameter("@tournament_id", selectedTournamentId.Value),
                    new NpgsqlParameter("@type_id", selectedTypeId.Value)
                });

                finalsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки финалов: {ex.Message}"); }
        }

        public void BackButton_Click(ComboBox tourComboBox, ComboBox yearComboBox, ComboBox typeComboBox, ComboBox tournamentComboBox, DataGrid finalsGrid, Action updateUI)
        {
            if (currentStep <= 0) return;

            currentStep--;
            finalsGrid.ItemsSource = null; 

            tourComboBox.IsEnabled = currentStep == 0;
            yearComboBox.IsEnabled = currentStep == 1;
            typeComboBox.IsEnabled = currentStep == 2;
            tournamentComboBox.IsEnabled = currentStep == 3;

            if (currentStep < 4) { selectedTournamentId = null; tournamentComboBox.SelectedItem = null; }
            if (currentStep < 3) { selectedTypeId = null; typeComboBox.SelectedItem = null; }
            if (currentStep < 2) { selectedYear = null; yearComboBox.SelectedItem = null; }
            if (currentStep < 1) { tourF = null; tourComboBox.SelectedItem = null; }

            if (currentStep == 3) LoadTournaments(tournamentComboBox);
            if (currentStep == 1) LoadYears(yearComboBox);

            updateUI?.Invoke();
        }

        public void ClearButton_Click(ComboBox tourComboBox, ComboBox yearComboBox, ComboBox typeComboBox, ComboBox tournamentComboBox, DataGrid finalsGrid, Action updateUI)
        {
            currentStep = 0;
            tourF = null;
            selectedYear = selectedTypeId = selectedTournamentId = null;

            tourComboBox.IsEnabled = true;
            yearComboBox.IsEnabled = typeComboBox.IsEnabled = tournamentComboBox.IsEnabled = false;

            tourComboBox.SelectedItem = null;
            yearComboBox.ItemsSource = null;
            typeComboBox.SelectedItem = null;
            tournamentComboBox.ItemsSource = null;
            finalsGrid.ItemsSource = null;

            updateUI?.Invoke();
        }

        private void LoadYears(ComboBox yearComboBox)
        {
            try
            {
                string table = tourF == "ATP" ? "male_finals" : "female_finals";
                var dt = db.ExecuteQuery($"SELECT DISTINCT year FROM voznesenskiy_ga.{table} ORDER BY year DESC");
                yearComboBox.DisplayMemberPath = "year";
                yearComboBox.SelectedValuePath = "year";
                yearComboBox.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки годов: {ex.Message}"); }
        }

        private void LoadTournaments(ComboBox tournamentComboBox)
        {
            try
            {
                if (!selectedYear.HasValue) return;

                string table = tourF == "ATP" ? "male_tournaments" : "female_tournaments";
                string prefix = tourF == "ATP" ? "male" : "female";

                string sql = $@"
                    SELECT DISTINCT t.id, t.name
                    FROM voznesenskiy_ga.{table} t
                    JOIN voznesenskiy_ga.{prefix}_finals f ON t.id = f.tournament_id
                    WHERE f.year = @year";

                var parameters = new List<NpgsqlParameter> { new NpgsqlParameter("@year", selectedYear.Value) };

                if (selectedTypeId.HasValue)
                {
                    sql += " AND t.type_id = @type_id";
                    parameters.Add(new NpgsqlParameter("@type_id", selectedTypeId.Value));
                }

                var dt = db.ExecuteQuery(sql, parameters.ToArray());
                tournamentComboBox.DisplayMemberPath = "name";
                tournamentComboBox.SelectedValuePath = "id";
                tournamentComboBox.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { showMessage($"Ошибка загрузки турниров: {ex.Message}"); }
        }
    }
}