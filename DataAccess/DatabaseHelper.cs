using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using Npgsql;
using TennisCatalog.Models;

namespace TennisCatalog
{
    public class DatabaseHelper
    {
        private readonly string connectionString;
        public string ConnectionString => connectionString;

        public DatabaseHelper()
        {
            connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        public DatabaseHelper(string connStr)
        {
            connectionString = connStr ?? ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }

        // метод для выборки данных (SELECT)
        public DataTable ExecuteQuery(string query, NpgsqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            return dt;
                        }
                    }
                }
            }
            catch (NpgsqlException ex) { throw new Exception("Ошибка запроса: " + ex.Message); }
        }

        public int ExecuteScalar(string query, NpgsqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        object result = cmd.ExecuteScalar();
                        return (result == DBNull.Value || result == null) ? 0 : Convert.ToInt32(result);
                    }
                }
            }
            catch (NpgsqlException ex) { throw new Exception("Ошибка Scalar: " + ex.Message); }
        }

        public int ExecuteNonQuery(string query, NpgsqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (NpgsqlException ex) { throw new Exception("Ошибка NonQuery: " + ex.Message); }
        }

        public int GetTourId(string tourName)
        {
            string sql = "SELECT voznesenskiy_ga.get_tour_id(@tour)";
            return ExecuteScalar(sql, new[] { new NpgsqlParameter("@tour", tourName) });
        }

        // Добавление матча в дневник
        public bool AddToDiary(int userId, int matchId, string tourType)
        {
            string checkSql = "SELECT COUNT(*) FROM voznesenskiy_ga.user_diary WHERE user_id = @uid " +
                "AND match_id = @mid AND tour_type = @tt";
            var checkParams = new[] {
                new NpgsqlParameter("@uid", userId),
                new NpgsqlParameter("@mid", matchId),
                new NpgsqlParameter("@tt", tourType.ToLower())
            };

            var dt = ExecuteQuery(checkSql, checkParams);
            long count = Convert.ToInt64(dt.Rows[0][0]);

            if (count > 0) return false;

            string insertSql = "INSERT INTO voznesenskiy_ga.user_diary (user_id, match_id, tour_type) " +
                "VALUES (@uid_ins, @mid_ins, @tt_ins)";
            var insertParams = new[] {
                new NpgsqlParameter("@uid_ins", userId),
                new NpgsqlParameter("@mid_ins", matchId),
                new NpgsqlParameter("@tt_ins", tourType.ToLower())
            };

            ExecuteNonQuery(insertSql, insertParams);
            return true;
        }

        public List<DiaryMatch> GetUserDiary(int userId)
        {
            List<DiaryMatch> results = new List<DiaryMatch>();
            string[] types = { "atp", "wta" };

            foreach (var type in types)
            {
                string prefix = (type == "atp") ? "male" : "female";
                string sql = $@"
                    SELECT d.id as diary_id, d.notes, d.user_rating, 
                           p1.full_name as p1, p2.full_name as p2, pw.full_name as winner, 
                           f.score, t.name as t_name
                    FROM voznesenskiy_ga.user_diary d
                    JOIN voznesenskiy_ga.{prefix}_finals f ON d.match_id = f.id
                    JOIN voznesenskiy_ga.{prefix}_players p1 ON f.player1_id = p1.id
                    JOIN voznesenskiy_ga.{prefix}_players p2 ON f.player2_id = p2.id
                    JOIN voznesenskiy_ga.{prefix}_players pw ON f.winner_id = pw.id
                    JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                    WHERE d.user_id = @uid AND d.tour_type = @tt";

                var dt = ExecuteQuery(sql, new[] {
                    new NpgsqlParameter("@uid", userId),
                    new NpgsqlParameter("@tt", type)
                });

                foreach (DataRow row in dt.Rows)
                {
                    results.Add(new DiaryMatch
                    {
                        DiaryId = Convert.ToInt32(row["diary_id"]),
                        Player1 = row["p1"].ToString(),
                        Player2 = row["p2"].ToString(),
                        Winner = row["winner"].ToString(),
                        Score = row["score"].ToString(),
                        TournamentName = row["t_name"].ToString(),
                        TourType = type.ToUpper(),
                        Notes = row["notes"] != DBNull.Value ? row["notes"].ToString() : "",
                        Rating = row["user_rating"] != DBNull.Value ? Convert.ToInt32(row["user_rating"]) : 0
                    });
                }
            }
            return results;
        }

        public void RemoveFromDiary(int diaryId)
        {
            string sql = "DELETE FROM voznesenskiy_ga.user_diary WHERE id = @id";
            ExecuteNonQuery(sql, new[] { new NpgsqlParameter("@id", diaryId) });
        }

        public void UpdateDiaryEntry(int diaryId, string notes, int rating)
        {
            string sql = @"UPDATE voznesenskiy_ga.user_diary 
                           SET notes = @notes, user_rating = @rate 
                           WHERE id = @id";
            var parameters = new[] {
                new NpgsqlParameter("@id", diaryId),
                new NpgsqlParameter("@notes", (object)notes ?? DBNull.Value),
                new NpgsqlParameter("@rate", rating)
            };
            ExecuteNonQuery(sql, parameters);
        }

        public DataRow GetDiaryDetails(int diaryId)
        {
            string sql = "SELECT notes, user_rating FROM voznesenskiy_ga.user_diary WHERE id = @id";
            var dt = ExecuteQuery(sql, new[] { new NpgsqlParameter("@id", diaryId) });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable GetEncyclopediaTitles()
        {
            string sql = "SELECT id, title FROM voznesenskiy_ga.encyclopedia_articles ORDER BY title";
            return ExecuteQuery(sql);
        }

        public DataRow GetArticleDetails(int articleId)
        {
            string sql = "SELECT content, external_url FROM voznesenskiy_ga.encyclopedia_articles WHERE id = @id";
            var dt = ExecuteQuery(sql, new[] { new NpgsqlParameter("@id", articleId) });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public Dictionary<string, int> GetPlayerStatsForCompare(int playerId, string tour)
        {
            var dict = new Dictionary<string, int>();
            string prefix = (tour == "WTA") ? "female" : "male";

            string query = $@"
                SELECT tt.name, COUNT(*) 
                FROM voznesenskiy_ga.{prefix}_finals f
                JOIN voznesenskiy_ga.{prefix}_tournaments t ON f.tournament_id = t.id
                JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                WHERE f.winner_id = @pid 
                GROUP BY tt.name";

            var dt = ExecuteQuery(query, new[] { new NpgsqlParameter("@pid", playerId) });
            foreach (DataRow row in dt.Rows)
            {
                dict[row[0].ToString()] = Convert.ToInt32(row[1]);
            }
            return dict;
        }
    }
}