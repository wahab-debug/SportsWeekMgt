using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class TeamController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        /* get all teams of latest session which are approved*/
        [HttpGet]
        public HttpResponseMessage getTeams() 
        {
            try 
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                var teamList = db.Teams.
                    Where( t=> t.session_id == latestSession.id && t.teamStatus==1).
                    Select(t => new
                {
                    t.Tname,
                    t.image_path,
                    t.teamid,
                    t.className
                }
            ).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, teamList);
            }
            catch 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);

            }
        }
        [HttpGet]
        public HttpResponseMessage getByTeamId(int teamid) {
            try
            {
                var teamList = db.Teams.
                    Where(t => t.teamid == teamid && t.teamStatus==1).
                    Select(t => new
                    {
                        t.Tname,
                        t.image_path,
                        t.teamid,
                        t.className,
                        t.session_id,
                        t.captain_id,
                        t.sport_id,
                        t.teamStatus
                    }
            ).ToList();
                
                return Request.CreateResponse(HttpStatusCode.OK, teamList);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);

            }
        }
        //add team to latest session and check if team name already exist
        [HttpPost]
        public HttpResponseMessage postTeam(Team team) 
        {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                var existingTeam = db.Teams.FirstOrDefault(t => t.Tname == team.Tname);
                if (existingTeam != null) 
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict);
                }
                team.session_id = latestSession.id;
                var query = db.Teams.Add(team);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);

            }
        }
        //update team name , class name , image and status of team
        [HttpPost]
        public HttpResponseMessage updateTeam(int id, Team team)
        {
            try
            {
                var result = db.Teams.Where(t=> t.teamid == id).FirstOrDefault();
                if (result == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
                result.Tname = team.Tname;
                result.className = team.className;
                result.image_path = team.image_path;
                result.teamStatus = team.teamStatus;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "updated");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

            }
        }
        /*[HttpDelete]
        public HttpResponseMessage deleteTeam(string id)
        {
            try
            {
                var user = db.Users.FirstOrDefault(u => u.registration_no == id);

                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
                }

                db.Users.Remove(user);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "User deleted successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }*/
    }
}