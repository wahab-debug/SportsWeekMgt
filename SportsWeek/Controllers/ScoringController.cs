using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    }
}