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
        //return users of applications
        [HttpGet]
        public HttpResponseMessage UserList()
        {
            try
            {
                var list = db.Users
                    .Select(u => new
                    {
                        u.id,
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
        // return user on basis of reg number. this is used as login purpose
        [HttpGet]
        public HttpResponseMessage getById(string id) {
            try
            {
                // Fetch the user with the given id from the database
                var user = db.Users
                    .Where(u => u.registration_no == id)
                    .Select(u => new
                    {
                        u.id,
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
        //post a new user habndle registration
        [HttpPost]
        public HttpResponseMessage PostUser(User user)
        {
            try
            {
                var existingUser = db.Users.FirstOrDefault(u => u.registration_no == user.registration_no);
                if (existingUser != null) 
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict);
                }
                var result = db.Users.Add(user);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

            }
        }
        //update user in user table on basis of reg number
        [HttpPost]
        public HttpResponseMessage updateUser(string id,User user)
        {
            try
            {
                var result = db.Users.FirstOrDefault(u => u.registration_no == id);
                if (result == null) {
                    return Request.CreateResponse(HttpStatusCode.NotFound);
                }
                result.name = user.name;
                result.role = user.role;
                result.registration_no = user.registration_no;
                
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "updated");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

            }
        }
        //delete a user on basis of reg number
        [HttpDelete]
        public HttpResponseMessage deleteUser(string id)
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
        }
        // return list of user with role Mod and sports that are assigned to them on basis of recent session
        [HttpGet]
        public HttpResponseMessage getEventManagers() 
        {
            try
            {
                var latestSession = db.Sessions.OrderByDescending(s => s.end_date).FirstOrDefault();
                var eventmanagers = from User in db.Users
                                    join SessionSport in db.SessionSports on User.id equals SessionSport.managed_by
                                    join sport in db.Sports on SessionSport.sports_id equals sport.id
                                    where User.role == "Mod" && SessionSport.session_id == latestSession.id
                                    select new 
                                    {
                                      User.name, 
                                      User.registration_no, 
                                      Sport = sport.game 
                                    };
                return Request.CreateResponse(HttpStatusCode.OK, eventmanagers);

            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}