using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace TennisCatalog
{
    public class AdminPlayersManager : BaseAdminManager
    {
        private string tourP;
        private int? playerId;

        public AdminPlayersManager(DatabaseHelper db, Action<string> showMessage) : base(db, showMessage) { }

        public void SetTour(string tour)
        {
            tourP = tour;
            playerId = null;
        }

        // Добавление нового игрока. 
        public void AddPlayer(TextBox fullNameTextBox, TextBox countryTextBox, DataGrid playersGrid)
        {
            string fullName = fullNameTextBox.Text.Trim();
            string country = countryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(tourP) || (tourP != "ATP" && tourP != "WTA"))
            {
                showMessage("Выберите тур (ATP или WTA) перед добавлением игрока!");
                return;
            }

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
            {
                showMessage("Введите корректное ФИО игрока (минимум 3 символа)!");
                return;
            }

            if (string.IsNullOrWhiteSpace(country) || country.Length < 2)
            {
                showMessage("Введите корректное название страны (минимум 2 символа)!");
                return;
            }

            try
            {
                string prefix = GetPrefix(tourP);

                string checkSql = $"SELECT id FROM voznesenskiy_ga.{prefix}_players WHERE full_name ILIKE @name";
                var checkDt = db.ExecuteQuery(checkSql, new[] { new NpgsqlParameter("@name", fullName) });

                if (checkDt.Rows.Count > 0)
                {
                    showMessage("Игрок с таким именем уже существует в базе данных!");
                    return;
                }

                string insertSql = $"INSERT INTO voznesenskiy_ga.{prefix}_players (full_name, country) VALUES (@name, @country)";
                var insertParams = new[]
                {
                    new NpgsqlParameter("@name", fullName),
                    new NpgsqlParameter("@country", country)
                };

                db.ExecuteNonQuery(insertSql, insertParams);

                showMessage("Игрок успешно добавлен в каталог!");

                ClearPlayers(fullNameTextBox, countryTextBox);
                LoadAllPlayers(playersGrid);
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка добавления игрока: {ex.Message}");
            }
        }

        public void DeletePlayer(TextBox fullNameTextBox, TextBox countryTextBox, DataGrid playersGrid)
        {
            if (playerId.HasValue)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить выбранного игрока?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ExecuteDelete(playerId.Value, playersGrid);
                }
            }
            else
            {
                string fullName = fullNameTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(tourP) || (tourP != "ATP" && tourP != "WTA"))
                {
                    showMessage("Выберите тур (ATP или WTA)!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    showMessage("Выберите игрока в таблице ИЛИ введите его точное имя для удаления!");
                    return;
                }

                try
                {
                    string prefix = GetPrefix(tourP);
                    string sql = $"SELECT id FROM voznesenskiy_ga.{prefix}_players WHERE full_name = @name_adj";
                    var dt = db.ExecuteQuery(sql, new[] { new NpgsqlParameter("@name_adj", fullName) });

                    if (dt.Rows.Count == 0)
                    {
                        showMessage("Игрок с указанным именем не найден!");
                        return;
                    }

                    int deletePlayerId = (int)dt.Rows[0]["id"];
                    if (MessageBox.Show($"Удалить игрока {fullName}?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        ExecuteDelete(deletePlayerId, playersGrid);
                    }
                }
                catch (Exception ex)
                {
                    showMessage($"Ошибка поиска игрока для удаления: {ex.Message}");
                }
            }
        }

        private void ExecuteDelete(int idToDelete, DataGrid grid)
        {
            try
            {
                string prefix = GetPrefix(tourP);
                string deleteSql = $"DELETE FROM voznesenskiy_ga.{prefix}_players WHERE id = @id";

                db.ExecuteNonQuery(deleteSql, new[] { new NpgsqlParameter("@id", idToDelete) });

                showMessage("Игрок успешно удалён!");
                playerId = null;
                LoadAllPlayers(grid);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("violates foreign key constraint") || ex.Message.Contains("внешнего ключа") 
                    || ex.Message.Contains("fkey"))
                {
                    showMessage("Ошибка: Невозможно удалить игрока, так как он участвует в сохраненных финалах!");
                }
                else
                {
                    showMessage($"Системная ошибка удаления: {ex.Message}");
                }
            }
        }

        public void SetPlayerId(DataRowView row)
        {
            playerId = row != null && row["id"] != DBNull.Value ? (int?)row["id"] : null;
        }

        public void LoadAllPlayers(DataGrid playersGrid)
        {
            if (string.IsNullOrEmpty(tourP)) return;
            try
            {
                string prefix = GetPrefix(tourP);
                string sql = $"SELECT id, full_name, country FROM voznesenskiy_ga.{prefix}_players ORDER BY full_name";

                var dt = db.ExecuteQuery(sql);
                playersGrid.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0) showMessage("Игроки для этого тура не найдены.");
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка загрузки игроков: {ex.Message}");
            }
        }

        public void FilterPlayersByName(string nameFilter, string countryFilter, DataGrid playersGrid)
        {
            if (string.IsNullOrEmpty(tourP)) return;
            try
            {
                string prefix = GetPrefix(tourP);
                string sql = $"SELECT id, full_name, country FROM voznesenskiy_ga.{prefix}_players";
                var conditions = new List<string>();
                var parameters = new List<NpgsqlParameter>();

                nameFilter = nameFilter?.Trim();
                countryFilter = countryFilter?.Trim();

                if (!string.IsNullOrWhiteSpace(nameFilter))
                {
                    conditions.Add("full_name ILIKE @name");
                    parameters.Add(new NpgsqlParameter("@name", $"%{nameFilter}%"));
                }
                if (!string.IsNullOrWhiteSpace(countryFilter))
                {
                    conditions.Add("country ILIKE @country");
                    parameters.Add(new NpgsqlParameter("@country", $"%{countryFilter}%"));
                }

                if (conditions.Count > 0)
                {
                    sql += " WHERE " + string.Join(" AND ", conditions);
                }
                sql += " ORDER BY full_name";

                var dt = db.ExecuteQuery(sql, parameters.ToArray());
                playersGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка фильтрации игроков: {ex.Message}");
            }
        }

        private void ClearPlayers(TextBox fullNameTextBox, TextBox countryTextBox)
        {
            fullNameTextBox.Text = "";
            countryTextBox.Text = "";
            playerId = null;
        }
    }
}