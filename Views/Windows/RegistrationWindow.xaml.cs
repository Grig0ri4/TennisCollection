using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Npgsql;

namespace TennisCatalog
{
    public partial class RegistrationWindow : Window
    {
        private readonly DatabaseHelper dbHelper;
        private byte[] photoData;

        public RegistrationWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
        }

        private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
                Title = "Выберите фотографию"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                photoData = File.ReadAllBytes(openFileDialog.FileName);
                PhotoPathTextBlock.Text = $"Выбрано: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;
            string firstName = FirstNameTextBox.Text;
            string lastName = LastNameTextBox.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Все поля должны быть заполнены!");
                return;
            }

            string checkQuery = "SELECT COUNT(*) FROM voznesenskiy_ga.users WHERE login = @login";
            var checkParams = new[] { new NpgsqlParameter("@login", login) };
            int count = dbHelper.ExecuteScalar(checkQuery, checkParams);
            if (count > 0)
            {
                MessageBox.Show("Такой логин уже существует!");
                return;
            }

            string passwordHash = HashPassword(password); 

            using (var conn = new NpgsqlConnection(dbHelper.ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertUserQuery = "INSERT INTO voznesenskiy_ga.users (login, password_hash) VALUES (@login, @password_hash) RETURNING id";
                        using (var cmd = new NpgsqlCommand(insertUserQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@login", login);
                            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                            int userId = (int)cmd.ExecuteScalar();

                            string insertProfileQuery = "INSERT INTO voznesenskiy_ga.user_profiles (userid, firstname, lastname) VALUES (@userid, @firstname, @lastname)";
                            using (var profileCmd = new NpgsqlCommand(insertProfileQuery, conn, transaction))
                            {
                                profileCmd.Parameters.AddWithValue("@userid", userId);
                                profileCmd.Parameters.AddWithValue("@firstname", firstName);
                                profileCmd.Parameters.AddWithValue("@lastname", lastName);
                                profileCmd.ExecuteNonQuery();
                            }

                            if (photoData != null)
                            {
                                string insertPhotoQuery = "INSERT INTO voznesenskiy_ga.photo_profiles (userid, photo) VALUES (@userid, @photo)";
                                using (var photoCmd = new NpgsqlCommand(insertPhotoQuery, conn, transaction))
                                {
                                    photoCmd.Parameters.AddWithValue("@userid", userId);
                                    photoCmd.Parameters.AddWithValue("@photo", photoData);
                                    photoCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        transaction.Commit();
                        MessageBox.Show("Регистрация успешна!");
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при регистрации: {ex.Message}");
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}