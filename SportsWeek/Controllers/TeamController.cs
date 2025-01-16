using SportsWeek.DTOs;
using SportsWeek.Models;
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
        //get a specific team details based on its team id
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
        //add team to latest session and check if team name already exist ----- 9/1/2025-- new logic for team add
        [HttpPost]
        public HttpResponseMessage postTeam(TeamDTOs teamlist) 
        {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.start_date).FirstOrDefault();
                if (latestSession == null || latestSession.start_date < DateTime.Now)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { errorcode = 1 });//, message = "No active session found." 
                }
                // Check if the team name already exists in the session
                if (db.Teams.Any(t => t.Tname == teamlist.Tname && t.session_id == latestSession.id))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new { errorcode = 3 });//, message = "Team with the same name already exists in this session."
                }
                //check if Caption exists in user table.
                var user = db.Users.FirstOrDefault(u => u.id == teamlist.captain_id);
                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { errorcode = 5 });//, message = "User not found." 
                }
                //check if the caption exists in student table.
                var student = db.Students.FirstOrDefault(s => s.reg_no == user.registration_no);
                if (student == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { errorcode = 6 });//, message = "Student not found."
                }

                var sport = db.Sports.FirstOrDefault(s => s.id == teamlist.sport_id);
                if (sport == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { errorcode = 7 });//, message = "Sport not found." 
                }

                // Check if the user is already a captain in the latest session for the same sport
                if (db.Teams.Any(t => t.captain_id == user.id && t.session_id == latestSession.id && t.sport_id == teamlist.sport_id))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new { errorcode = 4 });//, message = "User is already a captain of a team for the same sport in this session."
                }
                var maxTeamofSport = db.SessionSports.Where(s => s.sports_id == teamlist.sport_id).First();
                //  ensure the number of teams for a given session does not exceed the predefined limit
                if (db.Teams.Where(s => s.session_id == latestSession.id && s.sport_id==teamlist.sport_id).Count()>maxTeamofSport.no_of_teams)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new { errorcode = 10 });//, message = "max team error when team are exceeded from max team limit"
                }

                // Create team
                var newTeam = new Team
                {
                    Tname = teamlist.Tname,
                    className = teamlist.className,
                    captain_id = teamlist.captain_id,
                    session_id = latestSession.id,
                    sport_id = teamlist.sport_id,
                    image_path = teamlist.Image_path,
                    teamStatus = teamlist.teamStatus,
                    teamGender = student.gender,
                };

                db.Teams.Add(newTeam);
                db.SaveChanges();


                // Add player data if SingleUser
                if (teamlist.TeamType == "SingleUser")
                {
                    var player = new Player
                    {
                        reg_no = student.reg_no,
                        team_id = newTeam.teamid
                    };

                    db.Players.Add(player);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.Created);
                }
                var response = new
                {
                    newTeam.teamid,
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                // Log the error (logging mechanism not shown here)
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
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
       //return list of teams for specific sport that is assigned to the manager from latest session where role is "MOD" as well
        [HttpGet]
        public HttpResponseMessage AllTeamsByEM(string emRegNo)
        {
            try
            {
                var emUserId = db.Users
                                 .Where(u => u.registration_no == emRegNo)
                                 .Select(u => u.id)
                                 .FirstOrDefault();
                var latestSession = db.Sessions.OrderByDescending(s => s.start_date).FirstOrDefault();

                if (emUserId == 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event Manager not found.");
                }

                var teams = (from team in db.Teams
                             join sessionSport in db.SessionSports on team.sport_id equals sessionSport.sports_id
                             join captain in db.Users on team.captain_id equals captain.id
                             where sessionSport.managed_by == emUserId && sessionSport.session_id == latestSession.id
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
        //gets name and id of teams that are playing match for a fixture id
        [HttpGet]
        public HttpResponseMessage playingTeams(int fixtureId) 
        {
            try
            {
                var fixture = db.Fixtures.Where(f => f.id == fixtureId).FirstOrDefault();

                // Fetch team names for both teams
                var team1name = db.Teams.Where(t1 => t1.teamid == fixture.team1_id).Select(t1 => new { t1.Tname, t1.teamid }).FirstOrDefault();
                var team2name = db.Teams.Where(t2 => t2.teamid == fixture.team2_id).Select(t2 => new {t2.Tname, t2.teamid}).FirstOrDefault();

                // Use DefaultIfEmpty to handle null values, setting a default value "No team" if the result is null
                var result = new
                {
                    Team1 = team1name.Tname ?? "Team undecided",
                    Team2 = team2name.Tname ?? "Team undecided",
                    team1Id = team1name.teamid,
                    team2Id = team2name.teamid
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }

        //get all teams of a user that he applied for in latest session
        [HttpGet]
        public HttpResponseMessage getUserAppliedTeams(int userId) 
        {
            try 
            {
                var result = db.Teams
                    .Where(p => p.captain_id == userId)
                    .Join(db.Sports,
                        team => team.sport_id,
                        sport => sport.id,
                        (team, sport) => new {
                            Tname = team.Tname,
                            teamStatus = team.teamStatus,
                            image_path = team.image_path,
                            className = team.className,
                            sport = sport.game // Include sport name here
                        })
                    .ToList();
                return Request.CreateResponse(HttpStatusCode.OK,result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}