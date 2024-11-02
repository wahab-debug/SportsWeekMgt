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
                    team1_name = t1.Tname,
                    team2_name = t2.Tname,
                    matchDate = f.matchDate,
                    venue = f.venue,
                    sport_name = s != null ? s.game : (s2 != null ? s2.game : null),
                    sport_type = s != null ? s.game_type : (s2 != null ? s2.game_type : null)
                };
                var results = fixturesQuery.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, results);

            }
            catch(Exception e) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}