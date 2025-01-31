using SportsWeek.DTOs;
using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class CricketScoringController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        //post match data
        /*     [HttpPost]
             public HttpResponseMessage AddCricketScore(delivery Score, string image_path)
             {
                 try
                 {
                     if (Score == null)
                     {
                         return Request.CreateResponse(HttpStatusCode.NotFound, "Score cannot be null.");
                     }

                     // Create a new delivery object
                     var data = new delivery
                     {
                         fixture_id = Score.fixture_id,
                         team_id = Score.team_id,
                         over_number = Score.over_number,
                         ball_number = Score.ball_number,
                         runs_scored = Score.runs_scored,
                         striker_id = Score.striker_id,
                         non_striker_id = Score.non_striker_id,
                         bowler_id = Score.bowler_id,
                         extras = Score.extras,
                         extra_runs = Score.extra_runs,
                         wicket_type = Score.wicket_type,
                         dismissed_player_id = Score.dismissed_player_id,
                         fielder_id = Score.fielder_id,
                     };

                     db.deliveries.Add(data);
                     db.SaveChanges();

                     // Handle image if provided
                     if (!string.IsNullOrEmpty(image_path))
                     {
                         var imagedata = new delivery_Image
                         {
                             image_path = image_path,
                             delivery_id = data.id // Use the saved delivery ID
                         };

                         db.delivery_Image.Add(imagedata);
                         db.SaveChanges();
                     }

                     return Request.CreateResponse(HttpStatusCode.OK);//s, "Cricket score added successfully."
                 }
                 catch (Exception ex)
                 {
                     return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
                 }
             }
     */

        [HttpPost]
        public HttpResponseMessage AddCricketScore(CricketbnbScoringDTO Score)
        {
            try
            {
                if (Score == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Score cannot be null.");
                }

                // Create and save delivery
                var data = new delivery
                {
                    fixture_id = Score.fixture_id,
                    team_id = Score.team_id,
                    over_number = Score.over_number,
                    ball_number = Score.ball_number,
                    runs_scored = Score.runs_scored,
                    striker_id = Score.striker_id,
                    non_striker_id = Score.non_striker_id,
                    bowler_id = Score.bowler_id,
                    extras = Score.extras,
                    extra_runs = Score.extra_runs,
                    wicket_type = Score.wicket_type,
                    dismissed_player_id = Score.dismissed_player_id == 0 ? (int?)null : Score.dismissed_player_id,
                    fielder_id = Score.fielder_id == 0 ? (int?)null : Score.fielder_id,
                };

                db.deliveries.Add(data);
                db.SaveChanges();

                // Handle image
                if (!string.IsNullOrEmpty(Score.image_Path))
                {
                    SaveBase64Image(Score.image_Path, data.id);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddMotm(ManOfTheMatch motm) {
            try
            {
                if (string.IsNullOrEmpty(motm.image_path))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Image data is required.");
                }

                var uploadsFolder = HttpContext.Current.Server.MapPath("~/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Extract image data
                var match = Regex.Match(motm.image_path, @"^data:image/(?<type>.+?);base64,(?<data>.+)$");
                if (!match.Success)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid image format.");
                }

                var base64Data = match.Groups["data"].Value;
                var bytes = Convert.FromBase64String(base64Data);
                var extension = match.Groups["type"].Value.Split('/').Last();
                var fileName = $"{Guid.NewGuid()}.{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                File.WriteAllBytes(filePath, bytes);

                // Save to database
                motm.image_path = $"/uploads/{fileName}"; // Assign image path
                db.ManOfTheMatches.Add(motm);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Man of the Match added successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private void SaveBase64Image(string base64String, int deliveryId)
        {
            try
            {
                var uploadsFolder = HttpContext.Current.Server.MapPath("~/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Extract image data
                var match = Regex.Match(base64String, @"^data:image/(?<type>.+?);base64,(?<data>.+)$");
                if (!match.Success) return;

                var base64Data = match.Groups["data"].Value;
                var bytes = Convert.FromBase64String(base64Data);
                var extension = match.Groups["type"].Value.Split('/').Last();
                var fileName = $"{Guid.NewGuid()}.{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                File.WriteAllBytes(filePath, bytes);

                // Save to database
                db.delivery_Image.Add(new delivery_Image
                {
                    image_path = $"/uploads/{fileName}",
                    delivery_id = deliveryId
                });
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the whole request
                System.Diagnostics.Trace.TraceError($"Image save failed: {ex.Message}");
            }
        }
        [HttpGet]
        public HttpResponseMessage getMatchScore(int matchId) {
            try 
            {
                var individualScore = (from d in db.deliveries
                                       join f in db.Fixtures on d.fixture_id equals f.id
                                       join p in db.Players on d.striker_id equals p.id
                                       join t in db.Teams on d.team_id equals t.teamid
                                       join s in db.Students on p.reg_no equals s.reg_no
                                       where f.id == matchId
                                       group d by new { f.id, d.striker_id, d.team_id, t.Tname, p.reg_no, s.name } into grouped
                                       select new
                                       {
                                           id = grouped.Key.id,
                                           teamid = grouped.Key.team_id,
                                           teamName = grouped.Key.Tname,
                                           striker_id = grouped.Key.striker_id,
                                           Batsman = grouped.Key.name,
                                           runs = grouped.Sum(d => d.runs_scored)
                                       }).ToList();
                var fixture = db.Fixtures.FirstOrDefault(d=>d.id==matchId);
                 var team1 = individualScore.Where(score => score.teamid == fixture.team1_id).ToList();
                 var team2 = individualScore.Where(score => score.teamid == fixture.team2_id).ToList();
                var EachTeamIndividualScore = new
                {
                    team1Score = team1,
                    team2Score = team2
                };
                var TeamRunswithExtra = (from d in db.deliveries
                                      join f in db.Fixtures on d.fixture_id equals f.id
                                      join t in db.Teams on d.team_id equals t.teamid
                                      where f.id == matchId
                                      group d by new { f.id, d.team_id, t.Tname } into grouped
                                      select new
                                      {
                                          id = grouped.Key.id,
                                          teamid = grouped.Key.team_id,
                                          teamName = grouped.Key.Tname,
                                          runs = grouped.Sum(d => d.runs_scored+d.extra_runs)
                                      }).ToList();
                var team1Runs = TeamRunswithExtra.Where(score => score.teamid == fixture.team1_id).ToList();
                var team2Runs = TeamRunswithExtra.Where(score => score.teamid == fixture.team2_id).ToList();
                var EachTeamScore = new
                {
                    team1Total = team1Runs,
                    team2Total = team2Runs
                };
                var bowlingStatsResponse = bowlingStatsPerMatch(matchId);
                var bowlingStatsResult = bowlingStatsResponse.Content.ReadAsAsync<object>().Result;

                var results = new 
                {
                    PlayersScore = EachTeamIndividualScore,
                    RunwithExtra = EachTeamScore,
                    bowlingStats = bowlingStatsResult,
                };

                return Request.CreateResponse(HttpStatusCode.OK,results);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            } 
        }
        //return top bowlers based on session id
        [HttpGet]
        public HttpResponseMessage topWicketTaker(int sessionId) {
            try 
            {
                var sessionSportIds = db.SessionSports
                                         .Where(ss => ss.session_id == sessionId)
                                         .Select(ss => ss.id)
                                         .ToList();
                // If sessionSportId is not found, return an error response
                if (sessionSportIds == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Session ID not found");
                }
                var result = (from d in db.deliveries
                              join p in db.Players on d.bowler_id equals p.id
                              join s in db.Students on p.reg_no equals s.reg_no
                              join f in db.Fixtures on d.fixture_id equals f.id
                              where sessionSportIds.Contains((int)f.sessionSport_id) // Only include fixtures with matching sessionSport_id
                              group d by new { d.bowler_id, s.name } into grouped
                              select new 
                              {
                                  bowlerName = grouped.Key.name,
                                  overs = grouped.Count()/6,
                                  scoresConc = grouped.Sum(d => d.runs_scored+d.extra_runs),
                                  wickets = grouped.Count(d=>d.wicket_type== "bowled" || d.wicket_type== "catch_out" || d.wicket_type== "stumped"),
                                 // economyRate = Math.Round((decimal)grouped.Sum(d => d.runs_scored + d.extra_runs) / (grouped.Count() / 6),1)  // Economy rate
                              }
                    ).OrderByDescending(d=>d.wickets)
                    .Take(10)
                    .ToList();
                if (result.Count==0) {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No matches played");
                }
                return Request.CreateResponse(HttpStatusCode.OK,result);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message); 
            }
        }
        //return top scorer based on session id
        [HttpGet]
        public HttpResponseMessage topScorer(int sessionId)
        {
            try
            {
                var sessionSportIds = db.SessionSports
                                         .Where(ss => ss.session_id == sessionId)
                                         .Select(ss => ss.id)
                                         .ToList();
                // If sessionSportId is not found, return an error response
                if (sessionSportIds == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Session ID not found");
                }
                var result = (from d in db.deliveries
                              join p in db.Players on d.striker_id equals p.id
                              join s in db.Students on p.reg_no equals s.reg_no
                              join f in db.Fixtures on d.fixture_id equals f.id
                              where sessionSportIds.Contains((int)f.sessionSport_id) // Only include fixtures with matching sessionSport_id
                              group d by new { d.striker_id, s.name } into grouped
                              select new
                              {
                                  bowlerName = grouped.Key.name,
                                  scores = grouped.Sum(d => d.runs_scored),
                                  oversPlayed = (grouped.Count()) / 6,  // Ensure the result is a double
                                  //strikeRate = Math.Round((double)(grouped.Sum(d => d.runs_scored)) / (double)(grouped.Count()) * 100, 2) // Ensure both values are double
                              }
                    ).OrderByDescending(d => d.scores)
                    .Take(10)
                    .ToList();
                if (result.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No matches played");
                }
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //return best player of tournament in session based search
        [HttpGet]
        public HttpResponseMessage BestPlayer(int sessionId)
        {
            try
            {
                var sessionSportIds = db.SessionSports
                        .Where(ss => ss.session_id == sessionId)
                        .Select(ss => ss.id)
                        .ToList();

                if (!sessionSportIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Session ID not found");
                }

                // Get batting statistics grouped by batsmen
                var battingStats = (from d in db.deliveries
                                    join p in db.Players on d.striker_id equals p.id
                                    join f in db.Fixtures on d.fixture_id equals f.id
                                    join s in db.Students on p.reg_no equals s.reg_no
                                    where sessionSportIds.Contains((int)f.sessionSport_id)
                                    group d by new { p.id, s.name } into g
                                    select new
                                    {
                                        PlayerId = g.Key.id,
                                        PlayerName = g.Key.name,
                                        BattingRuns = g.Sum(x => x.runs_scored),
                                        BallsFaced = g.Count()
                                    }).ToList();

                // Get bowling statistics grouped by bowlers
                var bowlingStats = (from d in db.deliveries
                                    join p in db.Players on d.bowler_id equals p.id
                                    join f in db.Fixtures on d.fixture_id equals f.id
                                    join s in db.Students on p.reg_no equals s.reg_no
                                    where sessionSportIds.Contains((int)f.sessionSport_id)
                                    group d by new { p.id, s.name } into g
                                    let totalMatches = g.Select(x => x.fixture_id).Distinct().Count()
                                    select new
                                    {
                                        PlayerId = g.Key.id,
                                        PlayerName = g.Key.name,
                                        WicketsTaken = g.Count(x => x.wicket_type == "bowled" ||
                                                                  x.wicket_type == "catch_out" ||
                                                                  x.wicket_type == "stumped"),
                                        RunsConceded = g.Sum(x => x.runs_scored + x.extra_runs),
                                        BallsBowled = g.Count(),
                                        MatchesPlayed = totalMatches
                                    }).ToList();

                // Combine batting and bowling stats
                var combinedStats = (from b in battingStats
                                     join bw in bowlingStats on b.PlayerId equals bw.PlayerId into bowlers
                                     from bw in bowlers.DefaultIfEmpty()
                                     let totalMatches = (bw != null ? bw.MatchesPlayed : 0) +
                                                      (b.BallsFaced > 0 ? 1 : 0)  // Count batting appearances
                                     select new
                                     {
                                         b.PlayerId,
                                         b.PlayerName,
                                         // Batting metrics
                                         b.BattingRuns,
                                         BattingStrikeRate = b.BallsFaced > 0 ?
                                             Math.Round((double)b.BattingRuns / b.BallsFaced * 100, 2) : 0,
                                         // Bowling metrics
                                         WicketsTaken = bw != null ? bw.WicketsTaken : 0,
                                         BowlingEconomy = bw != null && bw.BallsBowled > 0 ?
                                             Math.Round((double)bw.RunsConceded / (bw.BallsBowled / 6), 2) : 0,
                                         // Match participation
                                         TotalMatches = totalMatches,
                                         // Weighted combined score
                                         CombinedScore = (
                                             (b.BattingRuns * 1) +
                                             (bw != null ? bw.WicketsTaken * 25 : 0) -
                                             (bw != null ? bw.RunsConceded * 0.5 : 0)
                                         ) * (1 + Math.Log(totalMatches))  // Logarithmic match multiplier
                                     }).ToList();

                // Best Bowler (Top 5 by wickets and economy)
                var bestBowlers = bowlingStats
                                    .Where(p => p.WicketsTaken > 0)
                                    .OrderByDescending(p => p.WicketsTaken / (double)p.MatchesPlayed)  // Wickets per match
                                    .ThenBy(p => p.RunsConceded / (double)p.MatchesPlayed)             // Economy per match
                                    .ThenByDescending(p => p.MatchesPlayed)                            // More matches
                                    .Take(5)
                                    .Select(p => new
                                    {
                                        p.PlayerName,
                                        p.WicketsTaken,
                                        p.RunsConceded,
                                        p.MatchesPlayed,
                                        BowlingAverage = Math.Round(p.WicketsTaken / (double)p.MatchesPlayed, 2),
                                        EconomyPerMatch = Math.Round((double)p.RunsConceded / (double)p.MatchesPlayed, 2)
                                    })
                                    .ToList();

                // Best Batsmen (Top 5 by runs and strike rate)
                var bestBatsmen = combinedStats
                    .Where(p => p.BattingRuns > 0)
                    .OrderByDescending(p => p.BattingRuns)
                    .ThenByDescending(p => p.BattingStrikeRate)
                    .Take(5)
                    .ToList();

                // Top 10 All-rounders (Combined performance)
                var topAllRounders = combinedStats
                    .OrderByDescending(p => p.CombinedScore)
                    .Take(10)
                    .ToList();

                var result = new
                {
                    BestBowlers = bestBowlers,
                    BestBatsmen = bestBatsmen,
                    TopAllRounders = topAllRounders
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //fetch bowlers and overs with wickets of a match
        [HttpGet]
        public HttpResponseMessage bowlingStatsPerMatch(int fixtureId)
        {
            try
            {
                // Step 1: Get bowler's team and calculate stats
                var bowStat = (from d in db.deliveries
                               where d.fixture_id == fixtureId
                               join p in db.Players on d.bowler_id equals p.id // Get bowler's team
                               group d by new { d.fixture_id, p.team_id, d.bowler_id } into grouped
                               let validDeliveries = grouped.Count(d =>
                                   d.extras != "Wide" && d.extras != "No-ball") // Case-sensitive check
                               select new
                               {
                                   fixture_id = grouped.Key.fixture_id,
                                   team_id = grouped.Key.team_id, // Bowler's team
                                   bowler_id = grouped.Key.bowler_id,
                                   runsConceeded = grouped.Sum(d => d.runs_scored + d.extra_runs),
                                   overs = (validDeliveries / 6) + (validDeliveries % 6) * 0.1, // e.g., 7 balls → 1.1
                                   wickets_taken = grouped.Count(d =>
                                       d.wicket_type == "bowled" ||
                                       d.wicket_type == "catch_out" ||
                                       d.wicket_type == "stumped")
                               }).ToList();
                // Step 2: Create temporary data for #bowInfo
                var bowInfo = (from t in db.Teams
                               join p in db.Players on t.teamid equals p.team_id
                               join s in db.Students on p.reg_no equals s.reg_no
                               select new
                               {
                                   teamid = t.teamid,
                                   Tname = t.Tname,
                                   reg_no = s.reg_no,
                                   player_id = p.id,
                                   player_name = s.name
                               }).ToList();

                // Step 3: Join #bowStat and #bowInfo
                var result = (from bi in bowInfo
                              join bs in bowStat on bi.player_id equals bs.bowler_id
                              select new
                              {
                                  team_id = bi.teamid,
                                  team_name = bi.Tname,
                                  player_id = bi.player_id,
                                  player_name = bi.player_name,
                                  runs_conceeded = bs.runsConceeded,
                                  overs = bs.overs,
                                  wickets_taken = bs.wickets_taken
                              }).ToList();
                var fixture = db.Fixtures.FirstOrDefault(d => d.id == fixtureId);
                var team1 = result.Where(score => score.team_id == fixture.team1_id).ToList();
                var team2 = result.Where(score => score.team_id == fixture.team2_id).ToList();
                var EachTeamIndividualStats = new
                {
                    team1Stats = team1,
                    team2Stats = team2
                };
                if (result.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No bowling data found for the given fixture");
                }

                return Request.CreateResponse(HttpStatusCode.OK, EachTeamIndividualStats);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


    }
}