using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using Npgsql;

namespace TennisCatalog
{
    public class AdminFinalsManager : BaseAdminManager
    {
        private string tourF;
        private int? finalId;

        public AdminFinalsManager(DatabaseHelper db, Action<string> showMessage) : base(db, showMessage) { }

        public void SetTour(string tour)
        {
            tourF = tour;
            finalId = null;
        }

        public void LoadTournamentsAndPlayers(ComboBox tournamentCombo, ComboBox player1Combo, ComboBox player2Combo, ComboBox winnerCombo)
        {
            if (string.IsNullOrEmpty(tourF) || (tourF != "ATP" && tourF != "WTA")) return;

            string prefix = GetPrefix(tourF);
            LoadCombo($"SELECT id, name FROM voznesenskiy_ga.{prefix}_tournaments ORDER BY name", tournamentCombo);
            LoadCombo($"SELECT id, full_name FROM voznesenskiy_ga.{prefix}_players ORDER BY full_name",
                player1Combo, player2Combo, winnerCombo);
        }

        // метод добавления финала
        public void AddFinal(TextBox yearTextBox, ComboBox tournamentCombo, ComboBox player1Combo, ComboBox player2Combo, ComboBox winnerCombo, TextBox scoreTextBox, DataGrid finalsGrid)
        {
            if (string.IsNullOrWhiteSpace(tourF)) { showMessage("Выберите тур (ATP/WTA)!"); return; }
            if (tournamentCombo.SelectedValue == null) { showMessage("Выберите турнир!"); return; }
            if (player1Combo.SelectedValue == null || player2Combo.SelectedValue == null || winnerCombo.SelectedValue == null)
            { showMessage("Выберите обоих игроков и победителя!"); return; }

            if (!int.TryParse(yearTextBox.Text.Trim(), out int year) || year < 1968 || year > DateTime.Now.Year + 1)
            {
                showMessage("Введите корректный год (например, 2024)!");
                return;
            }

            string score = scoreTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(score) || score.Length < 3)
            {
                showMessage("Введите корректный счёт матча!");
                return;
            }

            if (!Regex.IsMatch(score, @"^[0-9\-\,\(\)\sWwOoRreEtT/\.]+$"))
            {
                showMessage("Счёт содержит недопустимые символы!\n\nРазрешены только цифры, тире, запятые, пробелы, скобки и тех. победы (W/O, ret.).\n\nПример: 6-4, 7-6(5)");
                return;
            }

            int player1Id = (int)player1Combo.SelectedValue;
            int player2Id = (int)player2Combo.SelectedValue;
            int winnerId = (int)winnerCombo.SelectedValue;
            int tournamentId = (int)tournamentCombo.SelectedValue;

            if (player1Id == player2Id)
            {
                showMessage("Игрок 1 и Игрок 2 не могут быть одним и тем же человеком!");
                return;
            }

            if (winnerId != player1Id && winnerId != player2Id)
            {
                showMessage("Победитель матча должен быть либо Игроком 1, либо Игроком 2!");
                return;
            }

            try
            {
                string prefix = GetPrefix(tourF);

                string checkSql = $"SELECT id FROM voznesenskiy_ga.{prefix}_finals WHERE year = @year AND tournament_id = @tourn";
                var checkParams = new[] { new NpgsqlParameter("@year", year), new NpgsqlParameter("@tourn", tournamentId) };

                if (db.ExecuteQuery(checkSql, checkParams).Rows.Count > 0)
                {
                    showMessage("Финал для этого года и турнира уже существует в базе!");
                    return;
                }

                string insertSql = $@"
                    INSERT INTO voznesenskiy_ga.{prefix}_finals 
                    (tour_id, year, tournament_id, player1_id, player2_id, winner_id, score) 
                    VALUES (
                        (SELECT id FROM voznesenskiy_ga.{prefix}_tours WHERE name = @tour), 
                        @year, @tourn, @p1, @p2, @win, @score
                    )";

                var insertParams = new[]
                {
                    new NpgsqlParameter("@tour", tourF),
                    new NpgsqlParameter("@year", year),
                    new NpgsqlParameter("@tourn", tournamentId),
                    new NpgsqlParameter("@p1", player1Id),
                    new NpgsqlParameter("@p2", player2Id),
                    new NpgsqlParameter("@win", winnerId),
                    new NpgsqlParameter("@score", score)
                };

                db.ExecuteNonQuery(insertSql, insertParams);

                showMessage("Финал успешно добавлен в каталог!");

                ClearFinals(yearTextBox, tournamentCombo, player1Combo, player2Combo, winnerCombo, scoreTextBox);
                LoadFilteredFinals(year.ToString(), tournamentCombo, finalsGrid);
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка добавления финала: {ex.Message}");
            }
        }

        // Метод удаления финала по ID
        public void DeleteFinal(TextBox yearTextBox, ComboBox tournamentCombo, DataGrid finalsGrid)
        {
            if (finalId.HasValue)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить выбранный финал?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ExecuteDelete(finalId.Value, finalsGrid, yearTextBox.Text, tournamentCombo);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(tourF) || string.IsNullOrWhiteSpace(yearTextBox.Text) || tournamentCombo.SelectedValue == null)
                {
                    showMessage("Выберите матч в таблице ИЛИ укажите год и турнир для удаления!");
                    return;
                }

                if (!int.TryParse(yearTextBox.Text.Trim(), out int year)) return;

                int tournamentId = (int)tournamentCombo.SelectedValue;
                try
                {
                    string prefix = GetPrefix(tourF);
                    string sql = $"SELECT id FROM voznesenskiy_ga.{prefix}_finals WHERE year = @year AND tournament_id = @tourn";
                    var dt = db.ExecuteQuery(sql, new[] { new NpgsqlParameter("@year", year), new NpgsqlParameter("@tourn", tournamentId) });

                    if (dt.Rows.Count == 0)
                    {
                        showMessage("Финал с указанными годом и турниром не найден!");
                        return;
                    }

                    int deleteFinalId = (int)dt.Rows[0]["id"];
                    if (MessageBox.Show($"Удалить финал {year} года?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        ExecuteDelete(deleteFinalId, finalsGrid, yearTextBox.Text, tournamentCombo);
                    }
                }
                catch (Exception ex) { showMessage($"Ошибка: {ex.Message}"); }
            }
        }

        private void ExecuteDelete(int idToDelete, DataGrid grid, string currentYear, ComboBox currentTournament)
        {
            string prefix = GetPrefix(tourF);
            string deleteSql = $"DELETE FROM voznesenskiy_ga.{prefix}_finals WHERE id = @id";
            db.ExecuteNonQuery(deleteSql, new[] { new NpgsqlParameter("@id", idToDelete) });

            showMessage("Финал удалён!");
            finalId = null;
            LoadFilteredFinals(currentYear, currentTournament, grid);
        }

        public void SetFinalId(DataRowView row)
        {
            finalId = row != null && row["id"] != DBNull.Value ? (int?)row["id"] : null;
        }

        public void LoadFilteredFinals(string yearText, ComboBox tournamentCombo, DataGrid finalsGrid)
        {
            if (string.IsNullOrEmpty(tourF) || string.IsNullOrWhiteSpace(yearText) || tournamentCombo.SelectedValue == null)
            {
                finalsGrid.ItemsSource = null;
                return;
            }

            if (!int.TryParse(yearText.Trim(), out int year)) return;

            int tournamentId = (int)tournamentCombo.SelectedValue;
            try
            {
                string prefix = GetPrefix(tourF);

                string sql = $@"
                    SELECT f.id, f.year, t.name AS tournament, p1.full_name AS player1, p2.full_name AS player2, pw.full_name AS winner, f.score
                    FROM voznesenskiy_ga.{prefix}_finals f
                    JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                    JOIN voznesenskiy_ga.{prefix}_players p1 ON f.player1_id = p1.id
                    JOIN voznesenskiy_ga.{prefix}_players p2 ON f.player2_id = p2.id
                    JOIN voznesenskiy_ga.{prefix}_players pw ON f.winner_id = pw.id
                    WHERE f.tour_id = @tour_id AND f.year = @year AND f.tournament_id = @tourn
                    ORDER BY f.year DESC";

                var parameters = new[]
                {
                    new NpgsqlParameter("@tour_id", db.GetTourId(tourF)),
                    new NpgsqlParameter("@year", year),
                    new NpgsqlParameter("@tourn", tournamentId)
                };

                var dt = db.ExecuteQuery(sql, parameters);
                finalsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка загрузки финалов: {ex.Message}");
            }
        }

        private void LoadCombo(string query, params ComboBox[] boxes)
        {
            try
            {
                var dt = db.ExecuteQuery(query);
                foreach (var cb in boxes)
                {
                    cb.ItemsSource = new DataView(dt);
                    cb.DisplayMemberPath = query.Contains("full_name") ? "full_name" : "name";
                    cb.SelectedValuePath = "id";
                }
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void ClearFinals(TextBox yearTextBox, ComboBox tournamentCombo, ComboBox player1Combo, ComboBox player2Combo, ComboBox winnerCombo, TextBox scoreTextBox)
        {
            yearTextBox.Text = "";
            tournamentCombo.SelectedIndex = -1;
            player1Combo.SelectedIndex = -1;
            player2Combo.SelectedIndex = -1;
            winnerCombo.SelectedIndex = -1;
            scoreTextBox.Text = "";
            finalId = null;
        }
    }
}