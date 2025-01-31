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
                                     }).OrderByDescending(d=>d.goalsScored).Take(5).ToList();

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
                                     essionSportId = ss.id,
                                     sportName = sp.game,
                                     eventManager = u.name,
                                     winnerId = f.winner_id,  // Assuming Fixtures table has winner_id
                                     loserId = f.team1_id == f.winner_id ? f.team2_id : f.team1_id, // Correctly determine loser
                                     winnerName = f.winner_id == f.team1_id ? t1.Tname : t2.Tname,
                                     loserName = f.winner_id == f.team1_id ? t2.Tname : t1.Tname

                                 }).ToList();

                // Step 2: Check if the result is found
                if (finalists == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No final match found for the given session ID");
                }

                // Step 3: Return the result
                return Request.CreateResponse(HttpStatusCode.OK, finalists);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during processing
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}