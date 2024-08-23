using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class SessionController : ApiController
    {
        SportsWeekDBEntities db = new SportsWeekDBEntities();
        [HttpGet]
        public HttpResponseMessage sessionList() 
        {
            try 
            {
                var list = db.Sessions.Select(s => new 
                {
                    s.name,
                    s.start,
                    s.end,
                }).OrderByDescending(s =>s.start).ToList();
                return Request.CreateResponse(HttpStatusCode.OK,list);
            }
            catch(Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage sessionAdd(Session session) 
        {
            try 
            {
                db.Sessions.Add(session);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "added");
            }
            catch (Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError,ex.Message);
            }
        }
    }
}