using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class MatchesController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        //get matches schedules of a sport between teams based on name
        [HttpGet]
        public HttpResponseMessage getMatches(string sportName)
        {
            try
            {
                var fixturesQuery =
                from f in db.Fixtures
                join t1 in db.Teams on f.team1_id equals t1.teamid
                join t2 in db.Teams on f.team2_id equals t2.teamid
                join s in db.Sports on t1.sport_id equals s.id into t1Sports
                from s in t1Sports.DefaultIfEmpty()
                join s2 in db.Sports on t2.sport_id equals s2.id into t2Sports
                from s2 in t2Sports.DefaultIfEmpty()
                where s.game == sportName || s2.game == sportName
                select new
                {
                    fixture_id = f.id,
                    team1_name = t1 != null ? t1.Tname : "Yet to Decide",  // If no team1, set to "Yet to Decide"
                    team2_name = t2 != null ? t2.Tname : "Yet to Decide",  // If no team2, set to "Yet to Decide"
                    matchDate = f.matchDate,
                    venue = f.venue,
                    winner_id = f.winner_id,
                    match_type = f.match_type,
                    sport_name = s != null ? s.game : (s2 != null ? s2.game : null),
                    sport_type = s != null ? s.game_type : (s2 != null ? s2.game_type : null)
                };
                var results = fixturesQuery.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, results);

            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }

        //set matches of a sport by creating complete schedule
        public HttpResponseMessage setSchedule(string EmRegNo, Fixture[] fixture) 
        {
            try 
            {
                // Get the latest session with manager id provoded bt frontend
                var eventManager = db.Users.FirstOrDefault(u => u.registration_no == EmRegNo);

                var latestSessionwithEM = db.SessionSports
                                      .Where(ss => ss.managed_by == eventManager.id)
                                      .OrderByDescending(ss => ss.session_id)
                                      .FirstOrDefault();

                // Add fixtures to the database
                foreach (var f in fixture)
                {
                    // Create a new Fixture object and set its properties
                    var newFixture = new Fixture
                    {
                        team1_id = f.team1_id,
                        team2_id = f.team2_id,
                        matchDate = f.matchDate,
                        venue = f.venue,
                        match_type = f.match_type,
                        winner_id = f.winner_id,   // Assuming winner_id is set, can be null if unknown
                        sessionSport_id = latestSessionwithEM.id  // Link to the latest session
                    };

                    // Add the new fixture to the Fixtures table
                    db.Fixtures.Add(newFixture);
                };

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
        //get all match schedules that are not filled yet where id is null for teams
        [HttpGet]
        public HttpResponseMessage AllScheduledFixtures(string emRegNo)
        {
            try
            {

                var eventManager = db.Users.FirstOrDefault(u => u.registration_no == emRegNo);


                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();

                var sessionsport = db.SessionSports
                    .FirstOrDefault(ss => ss.managed_by == eventManager.id && ss.session_id == latestSession.id);

                var unresolvedFixtures = db.Fixtures
                    .Select(f => new
                    {
                        f.id,
                        f.team1_id,
                        f.team2_id,
                        f.matchDate,
                        f.venue,
                        f.sessionSport_id,
                        f.match_type,
                        f.winner_id
                    })
                    .Where(f => f.winner_id == null && f.sessionSport_id == sessionsport.id)
                    .ToList();


                return Request.CreateResponse(HttpStatusCode.OK, unresolvedFixtures);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred: " + ex.Message);
            }
        }
        //update names of teams in schedule
        [HttpPut]
        public HttpResponseMessage UpdateFixture(Fixture[] fixtures)
        {
            try
            {
                foreach (var fixture in fixtures)
                {
                    // Fetch team1_id and team2_id based on team names
                    var team1 = db.Teams
                                     .Where(t => t.teamid == fixture.team1_id) // Assuming fixture has team1Name
                                     .FirstOrDefault();

                    var team2 = db.Teams
                                     .Where(t => t.teamid == fixture.team2_id) // Assuming fixture has team2Name
                                     .FirstOrDefault();

                    // Fetch the fixture from the database by fixture id
                    var fixtureToUpdate = db.Fixtures
                                             .FirstOrDefault(f => f.id == fixture.id); // Assuming fixture has id

                    if (team1 != null && team2 != null && fixtureToUpdate != null)
                    {
                        // Update team1_id, team2_id, and set winner_id to 0
                        fixtureToUpdate.team1_id = team1.teamid;
                        fixtureToUpdate.team2_id = team2.teamid;

                        // Save changes for the current fixture
                        db.SaveChanges();
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Fixtures updated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}