namespace TennisCatalog.Models
{
    public class DiaryMatch
    {
        public int DiaryId { get; set; }
        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string Winner { get; set; }
        public string Score { get; set; }
        public string TourType { get; set; }
        public string TournamentName { get; set; }
        public string Notes { get; set; } 
        public int Rating { get; set; }  
    }
}