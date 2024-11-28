﻿using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
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
        //get a specific team based on its id
        [HttpGet]
        public HttpResponseMessage getByTeamId(int teamid) {
            try
            {
                var teamList = db.Teams.
                    Where(t => t.teamid == teamid).
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
       //return list of teams for specific sport that is assigned to the manager
        [HttpGet]
        public HttpResponseMessage AllTeamsByEM(string emRegNo)
        {
            try
            {
                var emUserId = db.Users
                                 .Where(u => u.registration_no == emRegNo)
                                 .Select(u => u.id)
                                 .FirstOrDefault();

                if (emUserId == 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event Manager not found.");
                }

                var teams = (from team in db.Teams
                             join sessionSport in db.SessionSports on team.sport_id equals sessionSport.sports_id
                             join captain in db.Users on team.captain_id equals captain.id
                             where sessionSport.managed_by == emUserId
                             select new
                             {
                                 Tname = team.Tname,
                                 captainRegNo = captain.registration_no,
                                 image_path = team.image_path,
                                 status = team.teamStatus,
                                 teamid = team.teamid,
                                 className = team.className
                             }).Distinct();

                var result = teams.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //approve team status to 1 and change role of user to captain who applied for team if role is not captain
        [HttpPost]
        public HttpResponseMessage ApproveTeamById([FromBody]int teamid)
        {
            try{
                   var team = db.Teams.FirstOrDefault(t => t.teamid == teamid);
                    if (team == null)
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Team not found.");
                    }

                    // Get the captain of the team
                    var captain = db.Users.FirstOrDefault(c => c.id == team.captain_id);

                    // If the captain exists, update the role
                    if (captain != null)
                    {
                        // Only update the captain role if it's not already set to "Captain"
                        if (captain.role != "Captain")
                        {
                            captain.role = "Captain";
                            db.Entry(captain).State = EntityState.Modified; // Mark as modified to update the role in DB
                        }
                    }

                    // Approve the team by updating the teamStatus to 1 (approved)
                    team.teamStatus = 1;

                    // Save changes to the database
                    db.SaveChanges();

                    // Return success response
                    return Request.CreateResponse(HttpStatusCode.OK, "Team approved successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //return number of teams allowed for specific sport of current session
        [HttpGet]
        public HttpResponseMessage allowedTeams(int userId) 
        {
            try 
            {
                // Query to find the number of teams for the latest session of the specified sport
                var query = (from ss in db.SessionSports
                             join s in db.Sports on ss.sports_id equals s.id
                             join ses in db.Sessions on ss.session_id equals ses.id
                             where ss.managed_by == userId
                             orderby ses.start_date descending
                             select ss.no_of_teams).FirstOrDefault();

                // If no result is found, return a 404 or appropriate response
                if (query == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No sessions found for the specified sport.");
                }

                // If a result is found, return the number of teams
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
    }
}