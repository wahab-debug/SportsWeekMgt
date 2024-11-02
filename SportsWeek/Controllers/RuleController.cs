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

        [HttpPost]
        public HttpResponseMessage updateRules(int sportId, Rule rule) 
        {
            try
            {
                var result = db.Rules.FirstOrDefault(r=>r.sport_id==sportId);
                if (result == null) 
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
                result.rule_of_game = rule.rule_of_game;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }


    }
}