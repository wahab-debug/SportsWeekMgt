using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class TopPerformerController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        //get top performer from each sport except criket currently just top goal scorer of session
        [HttpGet]
        public HttpResponseMessage topPerformers(int sessionId)
        {
            try
            {
                // Retrieve the session obj
                var sessionSportIds = db.SessionSports
                                         .Where(ss => ss.session_id == sessionId)
                                         .Select(ss => ss.id)
                                         .ToList();
                if (sessionSportIds == null) 
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,"No session found.");
                }

                // Query for the relevant fixtures based on the sport name
                var TopGoalScorer = (from me in db.Match_Events
                                     join p in db.Players on me.player_id equals p.id
                                     join s in db.Students on p.reg_no equals s.reg_no
                                     where me.event_type == "goal" && sessionSportIds.Contains((int)me.sessionSport_id)
                                     group me by new { me.player_id, s.name } into grouped
                                     select new
                                     {
                                         playerId = grouped.Key.player_id,
                                         PlayerName = grouped.Key.name,
                                         goalsScored = grouped.Count()
                                     }).OrderByDescending(d=>d.goalsScored).Take(3).ToList();

                // Return the match list as a response
                return Request.CreateResponse(HttpStatusCode.OK, TopGoalScorer);
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }
        //get finalists with winner and runner up key with event manager and sport name
        [HttpGet]
        public HttpResponseMessage sessionGistforAdmin(int sessionId)
        {
            try
            {
                // Retrieve finalists (winners and runners-up) based on event results
                var finalists = (from ss in db.SessionSports
                                 join f in db.Fixtures on ss.id equals f.sessionSport_id
                                 join sp in db.Sports on ss.sports_id equals sp.id
                                 join u in db.Users on ss.managed_by equals u.id
                                 join t1 in db.Teams on f.team1_id equals t1.teamid
                                 join t2 in db.Teams on f.team2_id equals t2.teamid
                                 where ss.session_id==sessionId && f.match_type == "final"
                                 select new
                                 {
                                     sessionSportId = ss.id,
                                     sportName = sp.game,
                                     eventManager = u.name,
                                     winnerId = f.winner_id,  // Assuming Fixtures table has winner_id
                                     loserId = f.team1_id == f.winner_id ? f.team2_id : f.team1_id, // Correctly determine loser
                                     winnerName = f.winner_id == f.team1_id ? t1.Tname : t2.Tname,
                                     loserName = f.winner_id == f.team1_id ? t2.Tname : t1.Tname

                                 }).ToList();
                CricketScoringController controller = new CricketScoringController();
                controller.ControllerContext = this.ControllerContext; // Fix: Share context
                var topscorer = controller.topScorer(sessionId).Content.ReadAsAsync<object>().Result;
                var besplayer = controller.BestPlayer(sessionId).Content.ReadAsAsync<object>().Result;
                var wickettaker = controller.topWicketTaker(sessionId).Content.ReadAsAsync<object>().Result;
                var topgoalscorerResponse = topPerformers(sessionId);
                var topgoalscorer = topgoalscorerResponse.Content.ReadAsAsync<List<dynamic>>().Result;
                var gist = new
                {
                    finalList = finalists,
                    topScorer = topscorer,
                    bestPlaye = besplayer,
                    wicketTaker = wickettaker,
                    goalScoere = topgoalscorer
                };

                // Step 2: Check if the result is found
                if (finalists == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No final match found for the given session ID");
                }

                // Step 3: Return the result
                return Request.CreateResponse(HttpStatusCode.OK, gist);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during processing
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //not complete yet
        [HttpGet]
        public HttpResponseMessage GetPlayerPerformance(string regNo, int sessionId)
        {
            try
            {
                // 1. Look up the player by registration number.
                var player = db.Players.FirstOrDefault(p => p.reg_no == regNo);
                if (player == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Player not found.");
                }

                // 2. Get all sports in the given session.
                //    (Assumes SessionSports has a navigation property to Sport.)
                var sessionSports = db.SessionSports
                                      .Include("Sport")
                                      .Where(ss => ss.session_id == sessionId)
                                      .ToList();

                // This list will hold performance details for each sport
                var performanceResults = new List<object>();

                // 3. For each sport in the session check the scoring type and calculate the player’s performance.
                foreach (var sessionSport in sessionSports)
                {
                    var sport = sessionSport.Sport;  // sport details from Sports table
                                                     // Get all fixtures for this session sport.
                    var fixtures = db.Fixtures
                                     .Where(f => f.sessionSport_id == sessionSport.id)
                                     .ToList();

                    // This list will store match-by-match performance for the current sport.
                    var matchPerformances = new List<object>();

                    // Process based on the sport’s scoring type.
                    if (sport.scoring_type == "Cricket")
                    {
                        // For cricket, we work with the deliveries table.
                        foreach (var fixture in fixtures)
                        {
                            // Look for deliveries where the player was either batting (striker)
                            // or bowling (bowler) in this fixture.
                            var deliveries = db.deliveries
                                               .Where(d => d.fixture_id == fixture.id &&
                                                      (d.striker_id == player.id || d.bowler_id == player.id))
                                               .ToList();
                            // If no record exists for this match, then skip.
                            if (!deliveries.Any())
                                continue;

                            // Sum runs only where the player was batting.
                            int runs = deliveries
                                        .Where(d => d.striker_id == player.id)
                                        .Sum(d => d.runs_scored ?? 0);

                            // Count wickets from deliveries where the player was bowling
                            // and a wicket was recorded.
                            int wickets = deliveries
                                           .Where(d => d.bowler_id == player.id && !string.IsNullOrEmpty(d.wicket_type))
                                           .Count();

                            matchPerformances.Add(new
                            {
                                FixtureId = fixture.id,
                                MatchDate = fixture.matchDate,
                                Venue = fixture.venue,
                                RunsScored = runs,
                                WicketsTaken = wickets
                            });
                        }
                    }
                    else if (sport.scoring_type == "goalbase")
                    {
                        // For goal-based sports, we check the MatchEvents table for events of type "Goal".
                        foreach (var fixture in fixtures)
                        {
                            var goalEvents = db.Match_Events
                                               .Where(me => me.fixture_id == fixture.id &&
                                                        me.player_id == player.id &&
                                                        me.event_type == "Goal")
                                               .ToList();

                            // Skip fixture if the player did not record any events.
                            if (!goalEvents.Any())
                                continue;

                            int goals = goalEvents.Count();

                            matchPerformances.Add(new
                            {
                                FixtureId = fixture.id,
                                MatchDate = fixture.matchDate,
                                Venue = fixture.venue,
                                GoalsScored = goals
                            });
                        }
                    }
                    else if (sport.scoring_type == "pointbase")
                    {
                        // For point-based sports, we check for events of type "Point-Scored".
                        foreach (var fixture in fixtures)
                        {
                            var pointEvents = db.Match_Events
                                                .Where(me => me.fixture_id == fixture.id &&
                                                         me.player_id == player.id &&
                                                         me.event_type == "Point Scored" || me.event_type== "Ace Serve")
                                                .ToList();

                            if (!pointEvents.Any())
                                continue;

                            // Here, we assume that each occurrence of a "Point-Scored" event
                            // gives the player one point. If your design uses a different logic (for example,
                            // storing the actual score in a field), adjust the aggregation accordingly.
                            int points = pointEvents.Count();

                            matchPerformances.Add(new
                            {
                                FixtureId = fixture.id,
                                MatchDate = fixture.matchDate,
                                Venue = fixture.venue,
                                PointsScored = points
                            });
                        }
                    }

                    // Only add sports in which the player actually participated in one or more matches.
                    if (matchPerformances.Any())
                    {
                        performanceResults.Add(new
                        {
                            SportId = sport.id,
                            SportName = sport.game,
                            ScoringType = sport.scoring_type,
                            Matches = matchPerformances
                        });
                    }
                }

                // Return the aggregated performance details.
                return Request.CreateResponse(HttpStatusCode.OK, performanceResults);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}