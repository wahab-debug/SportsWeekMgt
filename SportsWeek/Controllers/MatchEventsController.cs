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
                var FixtureImg = new FixturesImage
                {
                    fixtures_id = (int)matchEvent.fixture_id,
                    image_path = ImgPath,
                    image_time = matchEvent.event_time,
                    event_id = matchEvent.id
                };

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
                db.FixturesImages.Add(FixtureImg);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, $"{matchEvent.id} matchEvents added successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}