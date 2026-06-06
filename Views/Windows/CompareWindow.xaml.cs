using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Npgsql;

namespace TennisCatalog
{
    public partial class CompareWindow : Window
    {
        private readonly DatabaseHelper _db;
        private readonly string _pfx;

        public CompareWindow(string tour)
        {
            InitializeComponent();

            _db = new DatabaseHelper();
            _pfx = (tour != null && tour.Contains("WTA")) ? "female" : "male";

            DataTable dt = _db.ExecuteQuery($"SELECT id, full_name FROM voznesenskiy_ga.{_pfx}_players ORDER BY full_name");
            P1List.ItemsSource = dt.DefaultView;
            P2List.ItemsSource = dt.Copy().DefaultView;
        }

        private void P1List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (P1List.SelectedItem is DataRowView row)
            {
                P1SearchBox.TextChanged -= P1SearchBox_TextChanged;
                P1SearchBox.Text = row["full_name"].ToString();
                P1SearchBox.TextChanged += P1SearchBox_TextChanged;
            }
            Update_Chart(); 
        }

        private void P2List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (P2List.SelectedItem is DataRowView row)
            {
                P2SearchBox.TextChanged -= P2SearchBox_TextChanged;
                P2SearchBox.Text = row["full_name"].ToString();
                P2SearchBox.TextChanged += P2SearchBox_TextChanged;
            }
            Update_Chart(); 
        }

        // Сравнение статистик двух игроков
        private void Update_Chart()
        {
            if (P1List.SelectedItem is DataRowView r1 && P2List.SelectedItem is DataRowView r2)
            {
                var s1 = GetS((int)r1["id"]);
                var s2 = GetS((int)r2["id"]);
                var lbls = new List<string>();
                var v1 = new List<double>();
                var v2 = new List<double>();

                var keys = new HashSet<string>(s1.Keys);
                foreach (var k in s2.Keys) keys.Add(k);

                foreach (var k in keys)
                {
                    lbls.Add(k);
                    v1.Add(s1.ContainsKey(k) ? s1[k] : 0);
                    v2.Add(s2.ContainsKey(k) ? s2[k] : 0);
                }

                CompareChart.Series = new ISeries[] {
                    new ColumnSeries<double> { Name = r1["full_name"].ToString(), Values = v1 },
                    new ColumnSeries<double> { Name = r2["full_name"].ToString(), Values = v2 }
                };
                CompareChart.XAxes = new Axis[] { new Axis { Labels = lbls.ToArray() } };
            }
        }

        private void P1SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (P1List.ItemsSource is DataView dv)
            {
                string filter = P1SearchBox.Text.Replace("'", "''");
                dv.RowFilter = string.IsNullOrWhiteSpace(filter) ? "" : $"full_name LIKE '%{filter}%'";
            }
        }

        private void P2SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (P2List.ItemsSource is DataView dv)
            {
                string filter = P2SearchBox.Text.Replace("'", "''");
                dv.RowFilter = string.IsNullOrWhiteSpace(filter) ? "" : $"full_name LIKE '%{filter}%'";
            }
        }

        private Dictionary<string, int> GetS(int id)
        {
            var d = new Dictionary<string, int>();

            string sql = $@"
                SELECT tt.name, COUNT(*) 
                FROM voznesenskiy_ga.{_pfx}_finals f 
                JOIN voznesenskiy_ga.{_pfx}_tournaments t ON f.tournament_id = t.id 
                JOIN voznesenskiy_ga.tournament_types tt ON t.type_id = tt.id
                WHERE f.winner_id = @id 
                GROUP BY tt.name";

            var dt = _db.ExecuteQuery(sql, new[] { new NpgsqlParameter("@id", id) });
            foreach (DataRow r in dt.Rows)
            {
                d[r[0].ToString()] = Convert.ToInt32(r[1]);
            }
            return d;
        }
    }
}