using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class MatchEventsController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();

        [HttpPost]
        public HttpResponseMessage AddMatchEvents(Match_Events matchEvent, string ImgPath)
        {
            try
            {
                var fixture = db.Fixtures.FirstOrDefault(f => f.id == matchEvent.fixture_id);

                int sessionSport_id = (int)fixture.sessionSport_id;
                var existingFixtureImg = db.FixturesImages.FirstOrDefault(f => f.image_path == ImgPath && f.fixtures_id == matchEvent.fixture_id);

                if (existingFixtureImg != null)
                {
                    // If it exists, update the existing record
                    existingFixtureImg.image_time = matchEvent.event_time;
                    existingFixtureImg.event_id = matchEvent.id;
                }
                else
                {
                    // If it doesn't exist, create a new FixturesImage entry
                    var FixtureImg = new FixturesImage
                    {
                        fixtures_id = (int)matchEvent.fixture_id,
                        image_path = ImgPath,
                        image_time = matchEvent.event_time,
                        event_id = matchEvent.id
                    };
                    db.FixturesImages.Add(FixtureImg);
                }

                var newMatchEvent = new Match_Events
                {
                    fixture_id = matchEvent.fixture_id,
                    event_time = matchEvent.event_time,
                    event_type = matchEvent.event_type,
                    event_description = matchEvent.event_description,
                    sessionSport_id = sessionSport_id,
                    player_id = matchEvent.player_id,
                    secondary_player_id = matchEvent.secondary_player_id,
                    fielder_id = matchEvent.fielder_id,

                };
                db.Match_Events.Add(newMatchEvent);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, $"{matchEvent.id} matchEvents added successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage getMatchEvents(int matchId)
        {
            try
            {
                var fixtureEventsWithImages = from e in db.Match_Events
                                              join f in db.FixturesImages on e.id equals f.event_id into eventImages
                                              from f in eventImages.DefaultIfEmpty()  // LEFT JOIN with FixturesImages
                                              join p1 in db.Players on e.player_id equals p1.id into player1
                                              from p1 in player1.DefaultIfEmpty()  // LEFT JOIN for player reg_no
                                              join s1 in db.Students on p1.reg_no equals s1.reg_no into student1
                                              from s1 in student1.DefaultIfEmpty()  // LEFT JOIN for player name
                                              join p2 in db.Players on e.secondary_player_id equals p2.id into player2
                                              from p2 in player2.DefaultIfEmpty()  // LEFT JOIN for secondary player reg_no
                                              join s2 in db.Students on p2.reg_no equals s2.reg_no into student2
                                              from s2 in student2.DefaultIfEmpty()  // LEFT JOIN for secondary player name
                                              join p3 in db.Players on e.fielder_id equals p3.id into fielder
                                              from p3 in fielder.DefaultIfEmpty()  // LEFT JOIN for fielder reg_no
                                              join s3 in db.Students on p3.reg_no equals s3.reg_no into student3
                                              from s3 in student3.DefaultIfEmpty()  // LEFT JOIN for fielder name
                                              where e.fixture_id == matchId  // specify the fixture_id here
                                              orderby e.event_time  // ordering by event time
                                              select new
                                              {
                                                  event_id = e.id,
                                                  fixture_id = e.fixture_id,
                                                  event_time = e.event_time,
                                                  event_type = e.event_type,
                                                  event_description = e.event_description,
                                                  sessionSport_id = e.sessionSport_id,
                                                  player_name = s1.name,  // Player name from Students table
                                                  secondary_player_name = s2.name,  // Secondary player name from Students table
                                                  fielder_name = s3.name,  // Fielder name from Students table
                                                  image_path = f.image_path,
                                                  image_time = f.image_time
                                              };


                return Request.CreateResponse(HttpStatusCode.OK, fixtureEventsWithImages);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}