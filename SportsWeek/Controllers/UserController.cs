using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class UserController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();

        [HttpGet]

        public HttpResponseMessage UserList()
        {
            try
            {
                var list = db.Users
                    .Select(u => new
                    {
                        u.name,
                        u.registration_no,
                        u.password,
                        u.role
                    }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, list);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage getById(string id) {
            try
            {
                // Fetch the user with the given id from the database
                var user = db.Users
                    .Where(u => u.registration_no == id)
                    .Select(u => new
                    {
                        u.name,
                        u.registration_no,
                        u.password,
                        u.role
                    })
                    .FirstOrDefault();

                if (user == null)
                {
                    // If no user is found, return a NotFound response
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User not found");
                }

                // Return the user details
                return Request.CreateResponse(HttpStatusCode.OK, user);
            }
            catch (Exception ex)
            {
                // Handle exceptions and return an InternalServerError response
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        
        public HttpResponseMessage PostUser(User user)
        {
            try
            {
                var result = db.Users.Add(user);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

            }
        }

        [HttpPost]
        public HttpResponseMessage LoginStd(User t)
        {
            var user = db.Users.FirstOrDefault(u => u.registration_no == t.registration_no);
            try
            {
                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Null user not found");
                }
                if (user.password == t.password)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Success");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Wrong password");
                }

            }

            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }
    }
}