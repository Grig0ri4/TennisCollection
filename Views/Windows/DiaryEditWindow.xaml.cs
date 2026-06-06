using System.Windows;

namespace TennisCatalog
{
    public partial class DiaryEditWindow : Window
    {
        public string Notes { get; private set; }
        public int Rating { get; private set; }

        public DiaryEditWindow(string currentNotes, int currentRating)
        {
            InitializeComponent();

            NotesBox.Text = currentNotes;

            RatingSlider.Value = currentRating > 0 ? currentRating : 5;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Notes = NotesBox.Text;
            Rating = (int)RatingSlider.Value;

            this.DialogResult = true; 
        }
    }
}