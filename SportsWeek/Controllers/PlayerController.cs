using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class PlayerController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        [HttpGet]
        public HttpResponseMessage getTeamPlayers(int teamId) 
        {
            try 
            {
                var players = db.Players.Where(p => p.team_id == teamId).Select(tp => new {
                tp.reg_no
                }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, players);
            }
            catch 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
        [HttpPost]
        public HttpResponseMessage postPlayer(Player player, int teamId) 
        {
            try
            {
                var haveTeam = db.Players.Where(p => p.team_id == player.team_id).FirstOrDefault();
                db.Players.Add(player);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public HttpResponseMessage studentList(int semsec) 
        {
            try 
            {
                var query = db.Students.Where(p => p.semeno == semsec).Select(s => new { s.name, s.reg_no }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK,query);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
    }
}