using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SportsWeek.DTOs
{
    //all games scoring dto which will handle all games differently
    public class CricketScoringDTO
    {
        public string TeamName { get; set; }
        public int Score { get; set; }
        public float Over { get; set; }
        public int Wickets { get; set; }
        public int FixtureId { get; set; }
    }
    public class GoalBaseDTO {
        public string teamName { get; set; }
        public int goals { get; set; } 
        public int fixture_id { get; set; }
    }

    public class PointBaseDTO
    {
        public string teamName { get; set; }
        public int setsWon { get; set; }
        public int fixture_id { get; set; }
    }

}