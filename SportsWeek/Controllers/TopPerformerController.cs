﻿using SportsWeek.Models;
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
        //return user teams and fixtures of current session
        [HttpGet]
        public HttpResponseMessage GetUserTeams(string regno)
        {
            try
            {
                if (regno == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid user ID.");
                }
                var Studentregno = db.Students.FirstOrDefault(s => s.reg_no == regno);
                if (Studentregno == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,"Student data not found.");//, "Reg-no not found in student table "
                }

                // Get the latest session
                var latestSession = db.Sessions.OrderByDescending(s => s.start_date).FirstOrDefault();
                if (latestSession == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No sessions found.");
                }

                // Get all teams the user is part of
                var userTeams = db.Players
                    .Where(p => p.reg_no == regno)
                    .Select(p => p.team_id)
                    .ToList();

                if (!userTeams.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User is not part of any team.");//, "User is not part of any team."
                }

                // Get fixtures for the user's teams
                var userFixtures = (
                                     from f in db.Fixtures
                                     join t1 in db.Teams on f.team1_id equals t1.teamid
                                     join t2 in db.Teams on f.team2_id equals t2.teamid
                                     // Join team1's sport with SessionSports from the latest session
                                     join ss1 in db.SessionSports on
                                         new { sports_id = (int)t1.sport_id, session_id = latestSession.id }
                                         equals new { sports_id = (int)ss1.sports_id, session_id = (int)ss1.session_id }
                                     join s1 in db.Sports on ss1.sports_id equals s1.id
                                     // Join team2's sport with SessionSports from the latest session
                                     join ss2 in db.SessionSports on
                                         new { sports_id = (int)t2.sport_id, session_id = latestSession.id }
                                         equals new { sports_id = (int)ss2.sports_id, session_id = (int)ss2.session_id }
                                     join s2 in db.Sports on ss2.sports_id equals s2.id
                                     where userTeams.Contains(t1.teamid) || userTeams.Contains(t2.teamid)
                                     select new
                                     {
                                         fixtureid = f.id,
                                         team1id = t1.teamid,
                                         team2id = t2.teamid,
                                         team1name = t1.Tname,
                                         team2name = t2.Tname,
                                         matchdate = f.matchDate,
                                         venue = f.venue,
                                         sportname = userTeams.Contains(t1.teamid) ? s1.game : s2.game,
                                         winnerteam = f.winner_id == null
                                             ? "NotDecided"
                                             : db.Teams.Where(t => t.teamid == f.winner_id)
                                                       .Select(t => t.Tname)
                                                       .FirstOrDefault()
                                     }).ToList();


                // Get all teams the user is part of, even if they don't have fixtures
                var allUserTeams = (from t in db.Teams
                                    where userTeams.Contains(t.teamid)
                                    select new
                                    {
                                        teamname = t.Tname,
                                        hasFixtures = db.Fixtures.Any(f => f.team1_id == t.teamid || f.team2_id == t.teamid) // Check if the team has any fixtures
                                    }).ToList();
                var studentdata = db.Students.
                                   Where(s => s.reg_no == regno).
                                   Select(s => new { s.name, s.reg_no, s.discipline, s.section, s.semeno });

                // Combine results
                var result = new
                {
                    Fixtures = userFixtures,
                    Teams = allUserTeams,
                    userData = studentdata
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetplayerPerformancebysession(string regNo, int sessionId)
        {
            try
            {


                var players = db.Players
                                .Where(p => p.reg_no == regNo)
                                .Select(p => new { p.id, p.team_id })
                                .ToList();

                if (!players.Any())
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Player not found.");
                }

                var playerTeamIds = players.Select(p => p.team_id).ToList();
                var playerIds = players.Select(p => p.id).ToList();

                var userFixtures = (
                    from f in db.Fixtures
                    join ss in db.SessionSports on f.sessionSport_id equals ss.id
                    where ss.session_id == sessionId &&
                          (playerTeamIds.Contains(f.team1_id ?? 0) || playerTeamIds.Contains(f.team2_id ?? 0))
                    select new { f.id }
                ).ToList();

                var fixtureIds = userFixtures.Select(f => f.id).ToList();

                var cricketStats = db.deliveries
                    .Where(d => fixtureIds.Contains(d.fixture_id ?? 0) &&
                               (playerIds.Contains(d.striker_id ?? 0) || playerIds.Contains(d.bowler_id ?? 0)))
                    .GroupBy(d => d.fixture_id)
                    .Select(g => new
                    {
                        Fixtureid = g.Key,
                        totalRuns = g.Sum(d => (d.striker_id.HasValue && playerIds.Contains(d.striker_id.Value)) ? (d.runs_scored ?? 0) : 0),
                        totalWickets = g.Count(d => d.bowler_id.HasValue && playerIds.Contains(d.bowler_id.Value) &&
                                                   d.wicket_type != null &&
                                                   (d.wicket_type == "Bowled" || d.wicket_type == "Stumped" ||
                                                    d.wicket_type == "Hit Wicket" || d.wicket_type == "Caught"))
                    })
                    .ToList();
                var FootballStats = db.Match_Events
                    .Where(m => fixtureIds.Contains(m.fixture_id ?? 0) &&
                               (playerIds.Contains(m.player_id ?? 0)))
                    .GroupBy(m => m.fixture_id)
                    .Select(g => new
                    {
                        Fixtureid = g.Key,
                        totalgoals = g.Count(m => m.player_id.HasValue && playerIds.Contains(m.player_id.Value) && m.event_type == "Goal")
                    })
                    .ToList();
                var totalCricketMatches = cricketStats.Count;   // Count unique fixture IDs in Cricket
                var totalFootballMatches = FootballStats.Count; // Count unique fixture IDs in Football
                var totalruns = cricketStats.Sum(d => d.totalRuns);
                var totalwickets = cricketStats.Sum(d => d.totalWickets);
                var FootballStat = FootballStats.Sum(m => m.totalgoals);

                var totaldata = new
                {
                    Crickettotalruns = totalruns,
                    Crickettotalwickets = totalwickets,
                    totalCricketMatches = totalCricketMatches,
                    totalFootballMatches = totalFootballMatches,
                    FootballGoals = FootballStat,
                    //cricketStats = cricketStats
                };

                return Request.CreateResponse(HttpStatusCode.OK, totaldata);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage Getplayerrunsbybymatches(string regNo, int sessionId)
        {
            try
            {
                var players = db.Players
                                .Where(p => p.reg_no == regNo)
                                .Select(p => new { p.id, p.team_id })
                                .ToList();

                if (!players.Any())
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Player not found.");
                }

                var playerTeamIds = players.Select(p => p.team_id).ToList();
                var playerIds = players.Select(p => p.id).ToList();

                // Fetch Fixtures & Sports Data Correctly
                var userFixtures = db.Fixtures
                    .Where(f => db.SessionSports.Any(ss => ss.id == f.sessionSport_id && ss.session_id == sessionId) &&
                                (playerTeamIds.Contains(f.team1_id ?? 0) || playerTeamIds.Contains(f.team2_id ?? 0)))
                    .Select(f => new
                    {
                        f.id,
                        Team1Name = db.Teams.Where(t => t.teamid == f.team1_id).Select(t => t.Tname).FirstOrDefault() ?? "Unknown",
                        Team2Name = db.Teams.Where(t => t.teamid == f.team2_id).Select(t => t.Tname).FirstOrDefault() ?? "Unknown",
                        SportId = db.SessionSports.Where(ss => ss.id == f.sessionSport_id)
                                                   .Select(ss => ss.sports_id)
                                                   .FirstOrDefault()
                    })
                    .ToList()  // ✅ Fetch fixtures first
                    .Select(f => new  // ✅ Now safely fetch SportName
                    {
                        f.id,
                        f.Team1Name,
                        f.Team2Name,
                        f.SportId,
                        SportName = db.Sports.Where(s => s.id == f.SportId)
                                             .Select(s => s.game)
                                             .FirstOrDefault() ?? "Unknown"
                    })
                    .ToList();

                var fixtureIds = userFixtures.Select(f => f.id).ToList();

                var cricketStats = db.deliveries
                    .Where(d => fixtureIds.Contains(d.fixture_id ?? 0) &&
                                (playerIds.Contains(d.striker_id ?? 0) || playerIds.Contains(d.bowler_id ?? 0)))
                    .GroupBy(d => d.fixture_id)
                    .Select(g => new
                    {
                        Fixtureid = g.Key,
                        totalRuns = g.Sum(d => (d.striker_id.HasValue && playerIds.Contains(d.striker_id.Value)) ? (d.runs_scored ?? 0) : 0),
                        totalWickets = g.Count(d => d.bowler_id.HasValue && playerIds.Contains(d.bowler_id.Value) &&
                                                    d.wicket_type != null &&
                                                    (d.wicket_type == "Bowled" || d.wicket_type == "Stumped" ||
                                                     d.wicket_type == "Hit Wicket" || d.wicket_type == "Caught"))
                    })
                    .ToList();
                var FootballStats = db.Match_Events
                    .Where(m => fixtureIds.Contains(m.fixture_id ?? 0) &&
                               (playerIds.Contains(m.player_id ?? 0)))
                    .GroupBy(m => m.fixture_id)
                    .Select(g => new
                    {
                        Fixtureid = g.Key,
                        totalgoals = g.Count(m => m.player_id.HasValue && playerIds.Contains(m.player_id.Value) && m.event_type == "Goal")
                    })
                    .ToList();

                var results = userFixtures
                    .Select(fixture => new
                    {
                        Fixtureid = fixture.id,
                        Team1Name = fixture.Team1Name,
                        Team2Name = fixture.Team2Name,
                        SportName = fixture.SportName,
                        totalRuns = fixture.SportName == "Cricket"
                          ? (cricketStats.FirstOrDefault(cs => cs.Fixtureid == fixture.id)?.totalRuns ?? 0)
                                   : 0,
                        totalWickets = fixture.SportName == "Cricket"
                               ? (cricketStats.FirstOrDefault(cs => cs.Fixtureid == fixture.id)?.totalWickets ?? 0)
                                      : 0,
                        totalGoals = fixture.SportName == "Football"
                                ? (FootballStats.FirstOrDefault(cs => cs.Fixtureid == fixture.id)?.totalgoals ?? 0)
                                             : 0

                    });

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}