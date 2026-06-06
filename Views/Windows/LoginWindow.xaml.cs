using System;
using System.Windows;
using Npgsql;
using TennisCatalog.Models;

namespace TennisCatalog
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseHelper dbHelper;

        public LoginWindow()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите логин и пароль!");
                    return;
                }

                string passwordHash = HashPassword(password);

                string query = "SELECT id FROM voznesenskiy_ga.users WHERE login = @login AND password_hash = @password_hash";
                var paramsArr = new[]
                {
                    new NpgsqlParameter("@login", username),
                    new NpgsqlParameter("@password_hash", passwordHash)
                };

                var dt = dbHelper.ExecuteQuery(query, paramsArr);
                if (dt.Rows.Count > 0)
                {
                    UserSession.CurrentUserId = Convert.ToInt32(dt.Rows[0]["id"]);
                    UserSession.CurrentUsername = username;

                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow == null)
                    {
                        mainWindow = new MainWindow();
                        mainWindow.Show();
                        Application.Current.MainWindow = mainWindow;
                    }
                    mainWindow.SetCurrentUser(username);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}");
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow reg = new RegistrationWindow();
            reg.ShowDialog();
        }
    }
}