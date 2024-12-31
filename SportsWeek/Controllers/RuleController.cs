using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class RuleController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        //return rule of specific sport
        [HttpGet]
        public HttpResponseMessage viewRules(int sportId) 
        {
            try 
            {
                var gameRule = db.Rules.Where( e=> e.sport_id == sportId).Select(s => new { s.rule_of_game }).FirstOrDefault();
                if (gameRule == null) 
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "no Rule for game yet");
                }
                return Request.CreateResponse(HttpStatusCode.OK,gameRule);

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
        //edit rule of specific sport
        [HttpPost]
        public HttpResponseMessage updateRules(Rule rule) 
        {
            try
            {
                var result = db.Rules.FirstOrDefault(r=>r.sport_id==rule.sport_id);
                if (result == null) 
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
                result.rule_of_game = rule.rule_of_game;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);

            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage viewRulesPerEM(int emid)
        {
            try
            {
                var session = db.SessionSports.Where(u => u.managed_by==emid).FirstOrDefault();
                var sportRule = db.Rules.Where(r => r.sport_id == session.sports_id).Select(p => new 
                {
                    sport = p.Sport.game,
                    rule_of_game = p.rule_of_game
                });
                var result = sportRule.ToList();
/*                var gameRule = db.Rules.Where(e => e.sport_id == sportId).Select(s => new { s.rule_of_game }).FirstOrDefault();
*/               
                if (result == null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "no Rule for game yet");
                }
                return Request.CreateResponse(HttpStatusCode.OK, result);

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}