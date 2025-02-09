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
    public class CricketbnbScoringDTO
    {
        public int fixture_id { get; set; }
        public int team_id { get; set; }
        public int over_number { get; set; }
        public int ball_number { get; set; }
        public int runs_scored { get; set; }
        public int striker_id { get; set; }
        public int non_striker_id { get; set; }
        public int bowler_id { get; set; }
        public string extras { get; set; }
        public int extra_runs { get; set; }
        public string wicket_type { get; set; }
        public int dismissed_player_id { get; set; }
        public int fielder_id { get; set; }
        public string image_Path { get; set; }


    }
    public class GoalBaseDTO {
        public int teamid { get; set; }
        public int goals { get; set; } 
        public int fixture_id { get; set; }
    }

    public class PointBaseDTO
    {
        public string teamName { get; set; }
        public int setsWon { get; set; }
        public int fixture_id { get; set; }
    }
    //turnbase dto
    public class TurnBaseDTO 
    {
        public int fixture_id { get; set; }
        public int winner_id { get; set; }
    }

    // DTO class
    public class BallByBallDto
    {
        public int Over { get; set; }
        public int Ball { get; set; }
        public string Striker { get; set; }
        public string NonStriker { get; set; }
        public string Bowler { get; set; }
        public int BatsmanRuns { get; set; }
        public int ExtraRuns { get; set; }
        public string ExtraType { get; set; }
        public bool IsWicket { get; set; }
        public string WicketType { get; set; }
        public string DismissedPlayer { get; set; }
        public string Fielder { get; set; }
        public int TeamId { get; set; } // Internal use only
    }
}