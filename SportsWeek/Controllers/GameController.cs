using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class GameController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        [HttpGet]
        public HttpResponseMessage gameBySession() {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(ses=>ses.start_date).FirstOrDefault();
                var query = (from ssp in db.SessionSports
                            join s in db.Sports on ssp.sports_id equals s.id
                            join ss in db.Sessions on ssp.session_id equals ss.id
                            where ss.start_date==latestSession.start_date
                            select new
                            {
                                s.game,
                                s.game_type,
                                ss.name,
                                ss.start_date,
                                ss.end_date
                            }).ToList();
               
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage getAllgames() {
            try
            {
                var games = db.Sports.Select(s => s.game).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, games);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage addGame(Sport game) {
            try
            {
               db.Sports.Add(game);
               db.SaveChanges();
               return Request.CreateResponse(HttpStatusCode.OK, "Game list is added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage gameAddToLatestSession(List<Sport> games) {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                if (latestSession == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No session found");
                }
                foreach (var g in games)
                {
                    g.id = latestSession.id;
                    db.Sports.Add(g);
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Game list is added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            } 
    }
}