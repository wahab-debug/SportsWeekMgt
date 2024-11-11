﻿using SportsWeek.Models;
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
        //get list of players of a team
        [HttpGet]
        public HttpResponseMessage getTeamPlayers(int teamId) 
        {
            try 
            {
                var playerteam = from player in db.Players
                                 join team in db.Teams on player.team_id equals team.teamid
                                 join student in db.Students on player.reg_no equals student.reg_no
                                 where player.team_id == teamId
                                 select new 
                                 {
                                     reg_no = player.reg_no,
                                     teamName = team.Tname,
                                     playerName = student.name

                                 };
                var players = playerteam.ToList();
                return Request.CreateResponse(HttpStatusCode.OK, players);
            }
            catch 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
        //Adds players to team with several checks of team, session and sport and max player count
        [HttpPost]
        [Route("api/player/addPlayer/{teamName}")]
        public HttpResponseMessage AddPlayer([FromBody] string[] players, string teamName)
        {

            try
            {

                // Fetch the latest session ID
                var latestSessionId = db.Sessions.OrderByDescending(s => s.end_date).Select(s => s.id).FirstOrDefault();

                // Fetch the team that matches the name and the latest session
                var team = db.Teams.FirstOrDefault(t => t.Tname == teamName && t.session_id == latestSessionId);

                // Check if team exists for the latest session
                if (team == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Team not found for the latest session.");
                }
                // Check if the team already has 12 or more players
                int currentPlayerCount = db.Players.Count(p => p.team_id == team.teamid);
                if (currentPlayerCount >= 12)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Team already has the maximum number of players (12). Cannot add more players.");
                }

                foreach (var playerRegNo in players)
                {
                    var existingPlayer = db.Players.FirstOrDefault(p => p.reg_no == playerRegNo && p.team_id == team.teamid);
                    if (existingPlayer != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, $"Player with registration number {playerRegNo} is already in the team.");
                    }

                    var existingPlayerInSameSportAndSession = (from p in db.Players
                                                               join t in db.Teams on p.team_id equals t.teamid
                                                               where p.reg_no == playerRegNo && t.sport_id == team.sport_id && t.session_id == team.session_id
                                                               select p).FirstOrDefault();

                    if (existingPlayerInSameSportAndSession != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.Conflict, $"Player with registration number {playerRegNo} is already part of another team in the same sport and session.");
                    }

                    var player = new Player
                    {
                        reg_no = playerRegNo,
                        team_id = team.teamid
                    };

                    db.Players.Add(player);
                }
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Players added");


            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //get student list based on semester
        [HttpGet]
        public HttpResponseMessage studentList(int semsec, string sec) 
        {
            try 
            {
                var query = db.Students.Where(p => p.semeno == semsec && p.section==sec).Select(s => new { s.name, s.reg_no }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK,query);
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
    }
}