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

        //set matches of a sport full schedule
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
    }
}