using Microsoft.Ajax.Utilities;
using SportsWeek.DTOs;
using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class ScoringController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        //get scores of both teams playing of a specific match
        [HttpGet]
        public HttpResponseMessage matchScores(int matchId)
        {
            try
            {
                // Fetch the fixture details by matchId
                var fixture = db.Fixtures.FirstOrDefault(m => m.id == matchId);

                if (fixture != null)
                {
                    var team1 = db.Teams.FirstOrDefault(t => t.teamid == fixture.team1_id);
                    var team2 = db.Teams.FirstOrDefault(t => t.teamid == fixture.team2_id);
                    var comments = db.Comments.FirstOrDefault(c => c.fixture_id == matchId);
                    
                    // Initialize a response object to hold the match details
                    var matchDetails = new
                    {
                        Fixture = new
                        {
                            fixture.id,
                            fixture.team1_id,
                            fixture.team2_id,
                            fixture.matchDate,
                            fixture.venue,
                            winner = fixture.winner_id,
                            // Add team names to the response object
                            Team1Name = team1 != null ? team1.Tname : "Team 1 not found",
                            Team2Name = team2 != null ? team2.Tname : "Team 2 not found",
                            Comments = comments !=null ? comments.comments : "No commentary"
                        },
                        ScoreDetails = new List<object>()
                    };

                    // Check if there's goal-based scoring data for this fixture (e.g., football, soccer)
                    var goalScore = db.GoalBaseScores
                                      .Where(g => g.fixture_id == matchId)
                                      .Select(g => new
                                      {
                                          TeamId = g.team_id,
                                          g.goals
                                      }).ToList();

                    if (goalScore.Any())
                    {
                        matchDetails.ScoreDetails.Add(new
                        {
                            Type = "Goal-Based Scoring",
                            Score = goalScore
                        });
                    }

                    // Check if there's cricket scoring data for this fixture (e.g., cricket)
                    var cricketScore = db.CricketScores
                                         .Where(c => c.fixture_id == matchId)
                                         .Select(c => new
                                         {
                                             TeamId = c.team_id,
                                             c.score,
                                             c.overs,
                                             c.wickets
                                         }).ToList();

                    if (cricketScore.Any())
                    {
                        matchDetails.ScoreDetails.Add(new
                        {
                            Type = "Cricket Scoring",
                            Score = cricketScore
                        });
                    }

                    // Check if there's point-based scoring data for this fixture (e.g., tennis, volleyball)
                    var pointScore = db.PointsBaseScores
                                       .Where(p => p.fixture_id == matchId)
                                       .Select(p => new
                                       {
                                           TeamId = p.team_id,
                                           p.setsWon
                                       }).ToList();

                    if (pointScore.Any())
                    {
                        matchDetails.ScoreDetails.Add(new
                        {
                            Type = "Point-Based Scoring",
                            Score = pointScore
                        });
                    }
                    var TurnScore = db.TurnBaseGames
                                       .Where(p => p.fixture_id == matchId)
                                       .Select(p => new
                                       {
                                           WinnerId = p.winner_id,
                                           LoserId = p.loser_id,
                                       }).ToList();

                    if (TurnScore.Any())
                    {
                        matchDetails.ScoreDetails.Add(new
                        {
                            Type = "Turn-Based Scoring",
                            Score = pointScore
                        });
                    }

                    // Return the match details along with the score details
                    if (matchDetails.ScoreDetails.Any())
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, matchDetails);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No scores available for this match.");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Fixture not found.");
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddOrUpdateCricketScore(CricketScoringDTO cric)
        {
            try
            {
                // Fetch team based on teamName
                var team = db.Teams.FirstOrDefault(t => t.Tname == cric.TeamName);

                if (team == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Team not found");
                }

                // Check if the fixture_id exists and if the team_id is part of the fixture
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == cric.FixtureId);

                if (fixture == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Fixture not found");
                }

                if (fixture.team1_id != team.teamid && fixture.team2_id != team.teamid)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The team is not part of the specified fixture");
                }

                // Check if the fixture_id already exists for the given team_id in the CricketScore table
                var existingScore = db.CricketScores.FirstOrDefault(cs => cs.fixture_id == cric.FixtureId && cs.team_id == team.teamid);

                if (existingScore != null)
                {
                    // Update existing record
                    existingScore.score = cric.Score;
                    existingScore.overs = cric.Over;
                    existingScore.wickets = cric.Wickets;

                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Cricket Score updated successfully");
                }
                else
                {
                    // Insert new record
                    var newCricketScore = new CricketScore
                    {
                        team_id = team.teamid,
                        score = cric.Score,
                        overs = cric.Over,
                        wickets = cric.Wickets,
                        fixture_id = cric.FixtureId
                    };

                    db.CricketScores.Add(newCricketScore);
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Cricket Score added with ID " + newCricketScore.id);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateCricketWinner([FromBody] int fixtureId)
        {
            try
            {
                var fixture = db.Fixtures.FirstOrDefault(d => d.id == fixtureId);
                if (fixture == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Fixture not found.");
                }
                var TeamRunswithExtra = (from d in db.deliveries
                                         join f in db.Fixtures on d.fixture_id equals f.id
                                         join t in db.Teams on d.team_id equals t.teamid
                                         where f.id == fixtureId
                                         group d by new { f.id, d.team_id, t.Tname } into grouped
                                         select new
                                         {
                                             id = grouped.Key.id,
                                             teamid = grouped.Key.team_id,
                                             teamName = grouped.Key.Tname,
                                             runs = grouped.Sum(d => d.runs_scored + d.extra_runs)
                                         }).ToList();
                var team1Runs = TeamRunswithExtra.FirstOrDefault(score => score.teamid == fixture.team1_id);
                var team2Runs = TeamRunswithExtra.FirstOrDefault(score => score.teamid == fixture.team2_id);
                var EachTeamScore = new
                {
                    team1Total = team1Runs.runs,
                    team2Total = team2Runs.runs
                };

                if (team1Runs == null || team2Runs == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Scores for one or both teams not found.");
                }

                if (team1Runs.runs > team2Runs.runs)
                {
                    fixture.winner_id = fixture.team1_id;
                }
                else if (team2Runs.runs > team1Runs.runs)
                {
                    fixture.winner_id = fixture.team2_id;
                }
                else
                {
                    fixture.winner_id = null;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, fixture.winner_id);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred: " + ex.Message);
            }
        }
/*
        [HttpPost]
        public HttpResponseMessage PostHighScorer(List<scorecard> cards)
        {
            try
            {
                if (cards == null || !cards.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);//, " Not Found Data"
                }
                foreach (var card in cards)
                {
                    var data = new scorecard
                    {
                        fixture_id = card.fixture_id,
                        team_id = card.team_id,
                        player_id = card.player_id,
                        score = card.score,
                        ball_consumed = card.ball_consumed,


                    };
                    db.scorecards.Add(data);
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }*/
        //GoalBasedScoring

        [HttpPost]
        public HttpResponseMessage AddOrUpdateGoalBasedScore(GoalBaseDTO gbd)
        {
            try
            {
                var team = db.Teams.FirstOrDefault(t => t.Tname == gbd.teamName);
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == gbd.fixture_id);

                if (fixture.team1_id != team.teamid && fixture.team2_id != team.teamid)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The team is not part of the specified fixture");
                }

                var existingScore = db.GoalBaseScores.FirstOrDefault(cs => cs.fixture_id == gbd.fixture_id && cs.team_id == team.teamid);

                if (existingScore != null)
                {
                    // Update existing record
                    existingScore.goals = gbd.goals;

                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Score updated successfully");
                }
                else
                {
                    // Insert new record
                    var newGoalBaseScore = new GoalBaseScore
                    {
                        team_id = team.teamid,
                        goals = gbd.goals,
                        fixture_id = gbd.fixture_id
                    };

                    db.GoalBaseScores.Add(newGoalBaseScore);
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Cricket Score added with ID " + newGoalBaseScore.id);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPut]
        public HttpResponseMessage UpdateGoalBasedWinner([FromBody] int fixtureId)
        {
            try
            {
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == fixtureId);

                var team1Goals = db.GoalBaseScores.FirstOrDefault(s => s.fixture_id == fixtureId && s.team_id == fixture.team1_id);
                var team2Goals = db.GoalBaseScores.FirstOrDefault(s => s.fixture_id == fixtureId && s.team_id == fixture.team2_id);

                if (team1Goals == null || team2Goals == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Scores for one or both teams not found.");
                }

                if (team1Goals.goals > team2Goals.goals)
                {
                    fixture.winner_id = fixture.team1_id;
                }
                else if (team2Goals.goals > team1Goals.goals)
                {
                    fixture.winner_id = fixture.team2_id;
                }
                else
                {
                    fixture.winner_id = 0;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Winner ID updated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred: " + ex.Message);
            }
        }

        //PointBasedScoring
        [HttpPost]
        public HttpResponseMessage AddOrUpdatePointBasedScore(PointBaseDTO pbd)
        {
            try
            {
                var team = db.Teams.FirstOrDefault(t => t.Tname == pbd.teamName);
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == pbd.fixture_id);

                if (fixture.team1_id != team.teamid && fixture.team2_id != team.teamid)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The team is not part of the specified fixture");
                }

                var existingScore = db.PointsBaseScores.FirstOrDefault(cs => cs.fixture_id == pbd.fixture_id && cs.team_id == team.teamid);

                if (existingScore != null)
                {
                    // Update existing record
                    existingScore.setsWon = pbd.setsWon;

                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Score updated successfully");
                }
                else
                {
                    // Insert new record
                    var newPointBaseScore = new PointsBaseScore
                    {
                        team_id = team.teamid,
                        setsWon = pbd.setsWon,
                        fixture_id = pbd.fixture_id
                    };

                    db.PointsBaseScores.Add(newPointBaseScore);
                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, "Score added with ID " + newPointBaseScore.team_id);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdatePointBasedWinner([FromBody]int fixtureId)
        {
            try
            {
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == fixtureId);

                var team1Sets = db.PointsBaseScores.FirstOrDefault(s => s.fixture_id == fixtureId && s.team_id == fixture.team1_id);
                var team2Sets = db.PointsBaseScores.FirstOrDefault(s => s.fixture_id == fixtureId && s.team_id == fixture.team2_id);

                if (team1Sets == null || team2Sets == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Scores for one or both teams not found.");
                }

                if (team1Sets.setsWon > team2Sets.setsWon)
                {
                    fixture.winner_id = fixture.team1_id;
                }
                else if (team2Sets.setsWon > team1Sets.setsWon)
                {
                    fixture.winner_id = fixture.team2_id;
                }
                else
                {
                    fixture.winner_id = 0;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Winner ID updated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred: " + ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage newMatchScores(int matchId)
        {
            try
            {
                // Fetch the fixture details by matchId
                var fixture = db.Fixtures.FirstOrDefault(m => m.id == matchId);

                if (fixture != null)
                {
                    var team1 = db.Teams.FirstOrDefault(t => t.teamid == fixture.team1_id);
                    var team2 = db.Teams.FirstOrDefault(t => t.teamid == fixture.team2_id);

                    // Initialize a list to hold all match details and scores
                    var matchDetails = new List<object>();

                    // Add fixture details as the first item
                    matchDetails.Add(new
                    {
                        FixtureId = fixture.id,
                        Team1Id = fixture.team1_id,
                        Team2Id = fixture.team2_id,
                        MatchDate = fixture.matchDate,
                        Venue = fixture.venue,
                        Team1Name = team1 != null ? team1.Tname : "Team 1 not found",
                        Team2Name = team2 != null ? team2.Tname : "Team 2 not found",
                    });

                    // Goal-Based Scoring
                    var goalScore = db.GoalBaseScores
                                      .Where(g => g.fixture_id == matchId)
                                      .Select(g => new
                                      {
                                          TeamId = g.team_id,
                                          g.goals
                                      }).ToList();

                    if (goalScore.Any())
                    {
                        matchDetails.Add(new
                        {
                            Type = "Goal-Based Scoring",
                            Score = goalScore
                        });
                    }

                    // Cricket Scoring
                    var cricketScore = db.CricketScores
                                         .Where(c => c.fixture_id == matchId)
                                         .Select(c => new
                                         {
                                             TeamId = c.team_id,
                                             c.score,
                                             c.overs,
                                             c.wickets
                                         }).ToList();

                    if (cricketScore.Any())
                    {
                        matchDetails.Add(new
                        {
                            Type = "Cricket Scoring",
                            Score = cricketScore
                        });
                    }

                    // Point-Based Scoring
                    var pointScore = db.PointsBaseScores
                                       .Where(p => p.fixture_id == matchId)
                                       .Select(p => new
                                       {
                                           TeamId = p.team_id,
                                           p.setsWon
                                       }).ToList();

                    if (pointScore.Any())
                    {
                        matchDetails.Add(new
                        {
                            Type = "Point-Based Scoring",
                            Score = pointScore
                        });
                    }

                    // Turn-Based Scoring
                    var turnScore = db.TurnBaseGames
                                      .Where(t => t.fixture_id == matchId)
                                      .Select(t => new
                                      {
                                          WinnerId = t.winner_id,
                                          LoserId = t.loser_id,
                                      }).ToList();

                    if (turnScore.Any())
                    {
                        matchDetails.Add(new
                        {
                            Type = "Turn-Based Scoring",
                            Score = turnScore
                        });
                    }

                    // Return the match details and scores
                    return Request.CreateResponse(HttpStatusCode.OK, matchDetails);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Fixture not found.");
                }
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}