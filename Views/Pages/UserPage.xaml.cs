using System.Windows.Controls;
using System.Windows;
using TennisCatalog.Models;

namespace TennisCatalog
{
    public partial class UserPage : Page
    {
        private readonly string _username;

        public UserPage() : this(null) { }

        public UserPage(string username)
        {
            InitializeComponent();
            _username = username;

            var db = new DatabaseHelper();
            var userManager = new UserPageManager(db, message => MessageBox.Show(message));
            userManager.LoadUserInfo(_username, FullNameText, UserPhoto);
            this.IsVisibleChanged += UserPage_IsVisibleChanged;
        }

        private void UserPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true) 
            {
                var db = new DatabaseHelper();
                var userManager = new UserPageManager(db, message => MessageBox.Show(message));
                userManager.LoadUserStats(UserSession.CurrentUserId, AvgRatingText, TotalMatchesText, TourPieChart);
            }
        }

        private void MyDiaryButton_Click(object sender, RoutedEventArgs e)
        {
            var db = new DatabaseHelper();
            var diaryWindow = new DiaryWindow(db, UserSession.CurrentUserId);
            diaryWindow.ShowDialog();
            var userManager = new UserPageManager(db, message => MessageBox.Show(message));
            userManager.LoadUserStats(UserSession.CurrentUserId, AvgRatingText, TotalMatchesText, TourPieChart);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUserId = 0;
            UserSession.CurrentUsername = null;
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Window.GetWindow(this)?.Close();
            });
        }
    }
}