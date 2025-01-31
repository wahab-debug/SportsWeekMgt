﻿using SportsWeek.Models;
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
        //view all games of current session that is being played
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
                                id = s.id,
                                game = s.game,
                                game_type = s.game_type,
                                name = ss.name,
                                start_date = ss.start_date,
                                end_date = ss.end_date,
                                sessionsportid = ssp.id
                            }).ToList();
               
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //get all games of whole application
        [HttpGet]
        public HttpResponseMessage getAllgames() {
            try
            {
                var games = db.Sports.Select(s => new { s.game, s.id } ).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, games);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //add games in whole application
        [HttpPost]
        public HttpResponseMessage addGame(Sport game) {
            try
            {
                var existingGame = db.Sports.FirstOrDefault(s => s.game == game.game);
                if (existingGame != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict);
                }
               db.Sports.Add(game);
               db.SaveChanges();
               return Request.CreateResponse(HttpStatusCode.OK, "Game list is added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //add game in current session to play in current session
        [HttpPost]
        public HttpResponseMessage gameAddToLatestSession(SessionSport game) 
        {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                var existingGame = db.SessionSports.FirstOrDefault(gs => gs.sports_id == game.sports_id && gs.managed_by == game.managed_by);
                var uniqueGameperSession = db.SessionSports.FirstOrDefault(gs => gs.sports_id == game.sports_id && gs.session_id == latestSession.id);
                if (latestSession == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No session found");
                }
                if (existingGame != null && game.session_id==latestSession.id) 
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                if (uniqueGameperSession!=null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                game.session_id = latestSession.id;
                db.SessionSports.Add(game);                
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Game list is added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //return type of sport for loading screen of scoring and scoreboard
        [HttpGet]
        public HttpResponseMessage getSportType(int matchid) {
            try
            {

                var result = (from f in db.Fixtures
                              join ss in db.SessionSports on f.sessionSport_id equals ss.id
                              join s in db.Sports on ss.sports_id equals s.id
                              where f.id == matchid
                              select s.scoring_type ).FirstOrDefault();

                if (result == null )
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { errorcode = 1 });
                }

                // Return the sport related to the match
                return Request.CreateResponse(HttpStatusCode.OK, result);

            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        //take sessionid and return sports offered in that session
        [HttpGet]
        public HttpResponseMessage gamesBySessionID(int sessionId)
        {
            try
            {
                var query = (from ssp in db.SessionSports
                             join s in db.Sports on ssp.sports_id equals s.id
                             join ss in db.Sessions on ssp.session_id equals ss.id
                             where ssp.session_id == sessionId
                             select new
                             {
                                 id = s.id,
                                 game = s.game,
                                 game_type = s.game_type,
                                 name = ss.name,
                                 start_date = ss.start_date,
                                 end_date = ss.end_date,
                                 sessionsportid = ssp.id
                             }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}