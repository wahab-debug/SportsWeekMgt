using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SportsWeek.DTOs
{
    public class TeamStats
    {
        public string Name { get; set; }
        public int TotalScore { get; set; }
        public List<PlayerStats> Players { get; set; } = new List<PlayerStats>();
    }

    public class PlayerStats
    {
        public int PlayerId { get; set; }
        public int RunsScored { get; set; }
        public int BallsFaced { get; set; }
        public int Fours { get; set; }
        public int Sixes { get; set; }
    }


    public class ExtrasStats
    {
        public int NoBalls { get; set; }
        public int Wides { get; set; }
        public int TotalExtras { get; set; }
    }

    public class CricketScoreboardResponse
    {
        public TeamStats Team1 { get; set; }
        public TeamStats Team2 { get; set; }
        public ExtrasStats Extras { get; set; }
    }
}