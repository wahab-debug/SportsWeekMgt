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
        SportsWeekdbEntities db = new SportsWeekdbEntities();
        [HttpGet]
        public HttpResponseMessage sessionList() 
        {
            try 
            {
                var list = db.Sessions.Select(s => new 
                {
                    s.name,
                    s.start_date,
                    s.end_date,
                }).OrderByDescending(s =>s.start_date).ToList();
                return Request.CreateResponse(HttpStatusCode.OK,list);
            }
            catch(Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage currentSession() {
            try
            {
                var list = db.Sessions.OrderByDescending(s => s.end_date).Select(s=>s.name).FirstOrDefault();
                return Request.CreateResponse(HttpStatusCode.OK, list);
            }
            catch (Exception ex)
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