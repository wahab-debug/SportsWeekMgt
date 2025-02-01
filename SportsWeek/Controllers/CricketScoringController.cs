﻿using SportsWeek.DTOs;
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

        [HttpPost]
        public HttpResponseMessage AddCricketScore(CricketbnbScoringDTO Score)
        {
            try
            {
                if (Score == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Score cannot be null.");
                }
                // Check if striker/non-striker is already dismissed
                var dismissalTypes = new List<string>
                                        {
                                            "Bowled",
                                            "Caught",
                                            "Run Out",
                                            "Stumped",
                                            "Hit Wicket"
                                        };
                // Check existing dismissals for striker and non-striker in this fixture
                bool isStrikerDismissed = db.deliveries
                    .Any(d =>
                        d.fixture_id == Score.fixture_id &&
                        d.dismissed_player_id == Score.striker_id &&
                        dismissalTypes.Contains(d.wicket_type)
                    );
                bool isNonStrikerDismissed = db.deliveries
                    .Any(d =>
                        d.fixture_id == Score.fixture_id &&
                        d.dismissed_player_id == Score.non_striker_id &&
                        dismissalTypes.Contains(d.wicket_type)
                    );
                if (isStrikerDismissed || isNonStrikerDismissed)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict,$"Striker/Non-striker already dismissed in fixture {Score.fixture_id}");
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
        //return top bowlers based on session id accoring to new way
        [HttpGet]
        public HttpResponseMessage topWicketTaker(int sessionId)
        {
            try
            {
                var sessionSportIds = db.SessionSports
                    .Where(ss => ss.session_id == sessionId)
                    .Select(ss => ss.id)
                    .ToList();

                if (!sessionSportIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No fixtures found");
                }

                var validDeliveries = db.deliveries
                    .Where(d => d.fixture_id.HasValue) // Ensure fixture_id is not null
                    .Join(db.Fixtures,
                        d => d.fixture_id.Value,       // Now safe to use .Value
                        f => f.id,
                        (d, f) => new { Delivery = d, Fixture = f })
                    .Where(x =>
                        x.Fixture.sessionSport_id.HasValue &&
                        sessionSportIds.Contains(x.Fixture.sessionSport_id.Value) // Fix here
                    )
                    .Where(x =>
                        x.Delivery.extras != "Wide" &&
                        x.Delivery.extras != "No-ball"
                    )
                    .ToList();

                var result = validDeliveries
                    .GroupBy(x => new { x.Delivery.bowler_id })
                    .Select(g =>
                    {
                        var bowler = db.Players
                            .Where(p => p.id == g.Key.bowler_id)
                            .Select(p => p.Student.name)
                            .FirstOrDefault();

                        int validBalls = g.Count();
                        int overs = validBalls / 6;
                        int balls = validBalls % 6;
                        int runsConceded = (int)g.Sum(x => x.Delivery.runs_scored + x.Delivery.extra_runs);
                        int wickets = g.Count(x =>
                            x.Delivery.wicket_type == "Bowled" ||
                            x.Delivery.wicket_type == "Caught" ||
                            x.Delivery.wicket_type == "Stumped" ||
                            x.Delivery.wicket_type == "lbw" ||
                            x.Delivery.wicket_type == "hit wicket" ||
                            x.Delivery.wicket_type == "Caught and Bowled"
                        );

                        decimal economyRate = (validBalls == 0)
                            ? 0
                            : Math.Round(runsConceded / (decimal)(validBalls / 6.0m), 2);

                        return new
                        {
                            bowlerId = g.Key.bowler_id,
                            bowlerName = bowler ?? "Unknown",
                            wickets,
                            oversFormatted = $"{overs}.{balls}",
                            runsConceded,
                            economyRate
                        };
                    })
                    .OrderByDescending(b => b.wickets)
                    .ThenBy(b => b.economyRate)
                    .Take(10)
                    .ToList();
                if (!result.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No wickets taken in this session");
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //fetch top scorer of a session accoring to new way
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
                if (!sessionSportIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No fixtures found for this session");
                }

                // Fetch valid deliveries (exclude wides/no-balls)
                var validDeliveries = db.deliveries
                                    .Join(db.Fixtures,
                                          d => d.fixture_id,  // Foreign key in deliveries
                                          f => f.id,          // Primary key in fixtures
                                          (d, f) => new { Delivery = d, Fixture = f })
                                    .Where(x => sessionSportIds.Contains((int)x.Fixture.sessionSport_id))
                                    .Where(x => x.Delivery.extras != "Wide" && x.Delivery.extras != "No-ball")
                                    .ToList();
                var result = validDeliveries
                               .GroupBy(x => new {
                                   x.Delivery.striker_id,
                                   x.Delivery.Player.Student.name
                               })
                               .Select(g => new
                               {
                                   batsmanId = g.Key.striker_id,
                                   batsmanName = g.Key.name,
                                   totalRuns = g.Sum(x => x.Delivery.runs_scored),
                                   ballsFaced = g.Count(),
                                   strikeRate = g.Count() == 0
                                       ? 0
                                       : Math.Round((double)g.Sum(x => x.Delivery.runs_scored) / g.Count() * 100, 2),
                                   oversPlayed = $"{g.Count() / 6}.{g.Count() % 6}"
                               })
                               .OrderByDescending(p => p.totalRuns)
                               .ThenByDescending(p => p.strikeRate)
                               .Take(10)
                               .ToList();

                if (!result.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No batting records found");
                }
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //return best player of tournament in session based search accoring to new way
        [HttpGet]
        public HttpResponseMessage BestPlayer(int sessionId)
        {
            try
            {
                // 1. Get sessionSport IDs
                var sessionSportIds = db.SessionSports
                    .Where(ss => ss.session_id == sessionId)
                    .Select(ss => ss.id)
                    .ToList();

                if (!sessionSportIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Session not found");
                }

                // 2. Get all fixture IDs in the session (primitive int)
                var fixtureIds = db.Fixtures
                    .Where(f => f.sessionSport_id.HasValue &&
                          sessionSportIds.Contains(f.sessionSport_id.Value))
                    .Select(f => f.id)
                    .ToList();

                if (!fixtureIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No fixtures in this session");
                }

                // 3. Get batting stats (using primitive fixture IDs)
                var battingStats = db.deliveries
                    .Where(d => d.fixture_id.HasValue &&
                          fixtureIds.Contains(d.fixture_id.Value) &&
                          d.extras != "Wide" &&
                          d.extras != "No-ball")
                    .GroupBy(d => d.striker_id)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        TotalRuns = g.Sum(d => d.runs_scored),
                        BallsFaced = g.Count(),
                        Innings = g.Select(d => d.fixture_id).Distinct().Count()
                    })
                    .ToList();

                // 4. Get bowling stats (using primitive fixture IDs)
                var bowlingStats = db.deliveries
                    .Where(d => d.fixture_id.HasValue &&
                          fixtureIds.Contains(d.fixture_id.Value) &&
                          d.extras != "Wide" &&
                          d.extras != "No-ball")
                    .GroupBy(d => d.bowler_id)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        Wickets = g.Count(d =>
                            d.wicket_type == "Bowled" ||
                            d.wicket_type == "Caught" ||
                            d.wicket_type == "Stumped" ||
                            d.wicket_type == "lbw" ||
                            d.wicket_type == "hit wicket" ||
                            d.wicket_type == "Caught and Bowled"),
                        RunsConceded = g.Sum(d => d.runs_scored + d.extra_runs),
                        BallsBowled = g.Count(),
                        Innings = g.Select(d => d.fixture_id).Distinct().Count()
                    })
                    .ToList();

                // 5. Combine stats and calculate scores
                var allPlayerIds = battingStats.Select(b => b.PlayerId)
                                              .Union(bowlingStats.Select(b => b.PlayerId))
                                              .Distinct()
                                              .ToList();

                var players = db.Players
                    .Where(p => allPlayerIds.Contains(p.id))
                    .Select(p => new { p.id, p.Student.name })
                    .ToList();

                var result = allPlayerIds
                    .Select(playerId =>
                    {
                        var bat = battingStats.FirstOrDefault(b => b.PlayerId == playerId);
                        var bowl = bowlingStats.FirstOrDefault(b => b.PlayerId == playerId);
                        var player = players.FirstOrDefault(p => p.id == playerId);

                        return new
                        {
                            PlayerId = playerId,
                            PlayerName = player?.name ?? "Unknown",
                            BattingRuns = bat?.TotalRuns ?? 0,
                            BattingStrikeRate = bat != null && bat.BallsFaced > 0 ?
                                Math.Round((double)bat.TotalRuns / bat.BallsFaced * 100, 2) : 0,
                            WicketsTaken = bowl?.Wickets ?? 0,
                            BowlingEconomy = bowl != null && bowl.BallsBowled > 0 ?
                                Math.Round((double)bowl.RunsConceded / (bowl.BallsBowled / 6.0), 2) : 0,
                            MatchesPlayed = (bat?.Innings ?? 0) + (bowl?.Innings ?? 0)
                        };
                    })
                    .OrderByDescending(p => (p.BattingRuns * 2) + (p.WicketsTaken * 25)) // Simple scoring logic
                    .Take(10)
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        //fetch bowlers and overs with wickets of a match accoring to new way
        [HttpGet]
        public HttpResponseMessage bowlingStatsPerMatch(int fixtureId)
        {
            try
            {
                // Step 1: Calculate bowling stats (database-friendly operations only)
                var bowStat = (
                    from d in db.deliveries
                    where d.fixture_id == fixtureId
                    join p in db.Players on d.bowler_id equals p.id // Get bowler's team
                    group d by new { d.fixture_id, p.team_id, d.bowler_id } into grouped
                    let validDeliveries = grouped.Count(d =>
                        d.extras != "Wide" && d.extras != "No-ball") // Case-sensitive check
                    select new
                    {
                        fixture_id = grouped.Key.fixture_id,
                        team_id = grouped.Key.team_id,
                        bowler_id = grouped.Key.bowler_id,
                        runsConceeded = grouped.Sum(d => d.runs_scored + d.extra_runs),
                        validDeliveries = validDeliveries, // Store for later formatting
                        wickets_taken = grouped.Count(d =>
                            d.wicket_type == "Bowled" ||
                            d.wicket_type == "Caught" ||
                            d.wicket_type == "Stumped")
                    }).ToList(); // Materialize here to switch to LINQ-to-Objects

                // Step 2: Format overs as a string in memory (after ToList)
                var formattedBowStat = bowStat.Select(bs =>
                {
                    int completedOvers = bs.validDeliveries / 6;
                    int balls = bs.validDeliveries % 6;
                    string overs = balls == 0
                        ? $"{completedOvers}"
                        : $"{completedOvers}.{balls}";

                    return new
                    {
                        bs.fixture_id,
                        bs.team_id,
                        bs.bowler_id,
                        bs.runsConceeded,
                        overs,
                        bs.wickets_taken
                    };
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
                              join bs in formattedBowStat on bi.player_id equals bs.bowler_id
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