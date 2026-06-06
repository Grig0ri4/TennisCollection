using System;
using System.Data;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Npgsql;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;

namespace TennisCatalog
{
    public class UserPageManager
    {
        private readonly DatabaseHelper db;
        private readonly Action<string> showMessage;

        public UserPageManager(DatabaseHelper db, Action<string> showMessage)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        }

        public void LoadUserInfo(string username, TextBlock fullNameText, System.Windows.Controls.Image userPhoto)
        {
            username = username ?? (Application.Current.MainWindow as MainWindow)?.CurrentUser;
            if (string.IsNullOrEmpty(username))
            {
                fullNameText.Text = "Неизвестно Неизвестно";
                userPhoto.Source = null;
                return;
            }

            try
            {
                string query = @"
                    SELECT up.firstname, up.lastname, pp.photo
                    FROM voznesenskiy_ga.users u
                    LEFT JOIN voznesenskiy_ga.user_profiles up ON u.id = up.userid
                    LEFT JOIN voznesenskiy_ga.photo_profiles pp ON u.id = pp.userid
                    WHERE u.login = @login";

                var dt = db.ExecuteQuery(query, new[] { new NpgsqlParameter("@login", username) });
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    fullNameText.Text = $"{(row["firstname"] != DBNull.Value ? row["firstname"].ToString() : "")} {(row["lastname"] != DBNull.Value ? row["lastname"].ToString() : "")}";

                    if (row["photo"] != DBNull.Value)
                    {
                        using (var ms = new MemoryStream((byte[])row["photo"]))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            userPhoto.Source = bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка загрузки данных пользователя: {ex.Message}");
            }
        }

        // Рассчёт средней оценки и количества матчей в дневнике пользователя
        public void LoadUserStats(int userId, TextBlock avgRatingText, TextBlock totalMatchesText, LiveChartsCore.SkiaSharpView.WPF.PieChart pieChart)
        {
            try
            {
                if (userId <= 0) return;

                int atpCount = 0, wtaCount = 0, totalMatches = 0;
                double ratingSum = 0;
                int ratedMatchesCount = 0;

                string query = @"
                    SELECT tour_type, COUNT(*) as cnt, AVG(NULLIF(user_rating, 0)) as avg_rating
                    FROM voznesenskiy_ga.user_diary
                    WHERE user_id = @uid
                    GROUP BY tour_type";

                var dt = db.ExecuteQuery(query, new[] { new NpgsqlParameter("@uid", userId) });

                foreach (DataRow row in dt.Rows)
                {
                    string tourType = row["tour_type"].ToString();
                    int count = Convert.ToInt32(row["cnt"]);

                    totalMatches += count;

                    if (tourType == "atp") atpCount = count;
                    if (tourType == "wta") wtaCount = count;

                    if (row["avg_rating"] != DBNull.Value)
                    {
                        ratingSum += Convert.ToDouble(row["avg_rating"]) * count;
                        ratedMatchesCount += count;
                    }
                }

                totalMatchesText.Text = $"Сохранено матчей: {totalMatches}";
                if (ratedMatchesCount > 0)
                {
                    avgRatingText.Text = (ratingSum / ratedMatchesCount).ToString("0.0");
                }
                else
                {
                    avgRatingText.Text = "-";
                }

                var chartSeries = new List<ISeries>();

                if (atpCount > 0)
                    chartSeries.Add(new PieSeries<int> { Values = new[] { atpCount }, Name = "Мужской тур (ATP)", Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.DodgerBlue) });

                if (wtaCount > 0)
                    chartSeries.Add(new PieSeries<int> { Values = new[] { wtaCount }, Name = "Женский тур (WTA)", Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.HotPink) });

                if (chartSeries.Count == 0)
                {
                    chartSeries.Add(new PieSeries<int>
                    {
                        Values = new[] { 1 },
                        Name = "Нет данных",
                        Fill = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.LightGray)
                    });
                }

                pieChart.Series = chartSeries;
            }
            catch (Exception ex)
            {
                showMessage($"Ошибка загрузки статистики: {ex.Message}");
            }
        }
    }
}