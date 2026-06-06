using System;

namespace TennisCatalog.Models
{
    public class UserDiary
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MatchId { get; set; }
        public string TourType { get; set; }
        public DateTime ViewDate { get; set; }
        public string Notes { get; set; }
        public int UserRating { get; set; }
        public string TournamentName { get; set; }
        public string Score { get; set; }
    }
}
