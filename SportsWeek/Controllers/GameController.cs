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
        SportsWeekDBEntities db = new SportsWeekDBEntities();
        [HttpGet]
        public HttpResponseMessage gameBySession() {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end).FirstOrDefault();
                var list = db.Sports.Where(sp=> sp.sessionid==latestSession.id).
                    Select(s => new
                {
                    s.game
                }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, list);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage gameAdd(List<Sport> games) {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end).FirstOrDefault();
                if (latestSession == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No session found");
                }
                foreach (var g in games)
                {
                    g.sessionid = latestSession.id;
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