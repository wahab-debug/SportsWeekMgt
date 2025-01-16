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
                // Retrieve the sport object based on the sport name
                var sport = db.Sports.FirstOrDefault(s => s.game == sportName);
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                if (sport == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Sport not found.");
                }

                // Query for the relevant fixtures based on the sport name
                var fixturesQuery =
                from f in db.Fixtures
                join t1 in db.Teams on f.team1_id equals t1.teamid into t1Teams
                from t1 in t1Teams.DefaultIfEmpty()
                join t2 in db.Teams on f.team2_id equals t2.teamid into t2Teams
                from t2 in t2Teams.DefaultIfEmpty()
                join ss in db.SessionSports on f.sessionSport_id equals ss.id
                join s in db.Sports on ss.sports_id equals s.id
                where s.game == sportName 
                select new
                {
                    fixture_id = f.id,
                    team1_name = f.team1_id == null ? "Yet to Decide" : (t1 != null ? t1.Tname : "Yet to Decide"),
                    team2_name = f.team2_id == null ? "Yet to Decide" : (t2 != null ? t2.Tname : "Yet to Decide"),
                    team1_id = f.team1_id,
                    team2_id = f.team2_id,
                    matchDate = f.matchDate,
                    venue = f.venue,
                    winner_id = f.winner_id,
                    match_type = f.match_type,
                    sport_name = s.game,
                    sport_type = s.game_type
                };

                // Execute the query and get the results
                var results = fixturesQuery.ToList();

                // Return the match list as a response
                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
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
        //make winner id 0 so that front end show that match is being played
        [HttpPut]
        public HttpResponseMessage startMatch(int fixtureId) 
        {
            try 
            {
                var fixture = db.Fixtures.Where(s=>s.id == fixtureId).FirstOrDefault();
                if (fixture.winner_id == null) {
                    fixture.winner_id = 0;
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
    }
}