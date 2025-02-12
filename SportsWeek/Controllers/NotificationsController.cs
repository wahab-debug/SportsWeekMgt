using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class NotificationsController : ApiController
    {
       SportsWeekdbEntities db = new SportsWeekdbEntities();

        [HttpPost]
        public HttpResponseMessage addNotification(Notification noti) 
        {
            try 
            {

                // Validate input
                if (noti == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Notification is null.");
                }
                var userExist = db.Users.FirstOrDefault(d=>d.id == noti.id);
                var matchExist = db.Fixtures.FirstOrDefault(f=>f.id == noti.fixture_id);
                if (userExist == null || matchExist == null) 
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound,"User or fixture not found");
                }

                    // Check if there is already a notification for the same user and fixture.
                    // This ensures that a user (say, user_id=1) with fixture_id=45 can only be added once.
                    var existingNotification = db.Notifications
                                                 .FirstOrDefault(n => n.user_id == noti.user_id &&
                                                                      n.fixture_id == noti.fixture_id);

                    if (existingNotification != null)
                    {
                        // If a notification exists, you might want to update it (for example, mark it as unread)
                        // or simply return a conflict message.
                        // Here, we update its isRead flag to 0 (unread) if needed.
                        existingNotification.isRead = 0;
                        db.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, "Notification already exists; updated the status.");
                    }
                    else
                    {
                        // Otherwise, add the new notification.
                        // Setting isRead to 0 ensures that the notification is marked as "unread" by default.
                        noti.isRead = 0;
                        db.Notifications.Add(noti);
                        db.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, "Notification added successfully.");
                    }
                
            }
            catch(Exception ex) 
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /*
         [HttpGet]
         public HttpResponseMessage deleteNotification(Notification noti) { }*/

        [HttpGet]
        public HttpResponseMessage fetchNotifications(int userId)
        {
            try
            {
                // -----------------------------------------------------------------------------------
                // Step 1: Retrieve User Notifications
                // -----------------------------------------------------------------------------------
                // Query the Notifications table to get all notifications that belong to the specified user.
                // This creates the base query that will be further enriched with related data.
                var userNotifications = db.Notifications
                    .Where(n => n.user_id == userId);

                // -----------------------------------------------------------------------------------
                // Step 2: Inner Join with Fixtures
                // -----------------------------------------------------------------------------------
                // Each notification is linked to a fixture (match) through the fixture_id.
                // An inner join is used here since every notification is expected to have an associated fixture.
                var notificationsWithFixtures = userNotifications
                    .Join(
                        db.Fixtures,                     // The table to join with (Fixtures)
                        notification => notification.fixture_id, // Key selector for notifications (foreign key)
                        fixture => fixture.id,            // Key selector for fixtures (primary key)
                        (notification, fixture) => new    // Projection: create an anonymous object combining both entities
                        {
                            Notification = notification,
                            Fixture = fixture
                        }
                    );

                // -----------------------------------------------------------------------------------
                // Step 3: Left Join for Team1 Details
                // -----------------------------------------------------------------------------------
                // For each fixture, retrieve the details of Team1 (home or primary team).
                // A left join is performed (using GroupJoin and SelectMany) because it is possible that a team may not exist.
                var team1Joined = notificationsWithFixtures
                    .GroupJoin(
                        db.Teams,                         // The Teams table
                        nf => nf.Fixture.team1_id,         // Key from the Fixture representing Team1's ID
                        team => team.teamid,               // Key from the Teams table
                        (nf, teams) => new { nf, teams }    // Group the matching teams (could be empty if not found)
                    )
                    .SelectMany(
                        x => x.teams.DefaultIfEmpty(),     // Use DefaultIfEmpty to ensure a left join (returns null if no match)
                        (x, team1) => new
                        {
                            x.nf.Notification,
                            x.nf.Fixture,
                            Team1 = team1                   // May be null if no matching team found
                        }
                    );

                // -----------------------------------------------------------------------------------
                // Step 4: Left Join for Team2 Details
                // -----------------------------------------------------------------------------------
                // Similarly, join to obtain details for Team2 (away or secondary team) from the Teams table.
                // This is again a left join to handle any missing team information.
                var team2Joined = team1Joined
                    .GroupJoin(
                        db.Teams,                         // The Teams table
                        temp => temp.Fixture.team2_id,     // Key from the Fixture representing Team2's ID
                        team => team.teamid,               // Key from the Teams table
                        (temp, teams) => new { temp, teams }  // Group the matching teams (could be empty if not found)
                    )
                    .SelectMany(
                        x => x.teams.DefaultIfEmpty(),     // Ensures that if no matching team is found, null is returned
                        (x, team2) => new
                        {
                            x.temp.Notification,
                            x.temp.Fixture,
                            x.temp.Team1,
                            Team2 = team2                // May be null if no matching team found
                        }
                    );

                // -----------------------------------------------------------------------------------
                // Step 5: Left Join for Winner Details
                // -----------------------------------------------------------------------------------
                // Next, join the data with the Teams table to get the winning team's details.
                // The Fixture table may store a winner_id, which is used to look up the winning team.
                // A left join is necessary here because the winner may not be decided yet (null/0).
                var winnerJoined = team2Joined
                    .GroupJoin(
                        db.Teams,                         // The Teams table
                        temp => temp.Fixture.winner_id,    // Key from the Fixture representing the winner's team ID
                        team => team.teamid,               // Key from the Teams table
                        (temp, winners) => new { temp, winners } // Group the matching winners (could be empty)
                    )
                    .SelectMany(
                        x => x.winners.DefaultIfEmpty(),   // Returns null if no winner team is found
                        (x, winner) => new
                        {
                            x.temp.Notification,
                            x.temp.Fixture,
                            x.temp.Team1,
                            x.temp.Team2,
                            Winner = winner              // May be null if winner is not set
                        }
                    );

                // -----------------------------------------------------------------------------------
                // Step 6: Final Projection
                // -----------------------------------------------------------------------------------
                // Finally, shape the resulting data into an anonymous object that includes:
                // - Notification details (ID, read status)
                // - Fixture details (ID, match type, date)
                // - Team names and IDs (with null checks to provide default messages if a team is missing)
                // - Winner details (with defaults if no winner has been determined)
                var result = winnerJoined
                    .Select(x => new
                    {
                        NotificationId = x.Notification.id,
                        FixtureId = x.Fixture.id,
                        IsRead = x.Notification.isRead,
                        // For Team1, check if the team exists; if not, show a default message.
                        Team1Name = x.Team1 != null ? x.Team1.Tname : "Team not found",
                        Team1Id = x.Team1 != null ? x.Team1.teamid : 0, // Using 0 as a default ID when team is missing
                                                                        // For Team2, check if the team exists; if not, show a default message.
                        Team2Name = x.Team2 != null ? x.Team2.Tname : "Team not found",
                        Team2Id = x.Team2 != null ? x.Team2.teamid : 0, // Using 0 as a default ID when team is missing
                        MatchType = x.Fixture.match_type,
                        MatchDate = x.Fixture.matchDate,
                        // For the Winner, check if a winning team is associated; otherwise, display a default message.
                        WinnerTeam = x.Winner != null ? x.Winner.Tname : "No winner yet",
                        WinnerId = x.Winner != null ? x.Winner.teamid : 0 // Default to 0 if no winner is set
                    })
                    .ToList(); // Execute the query and convert the results into a list

                // Return the final list of notifications and their enriched details as an HTTP 200 OK response.
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                // If any error occurs during query execution or data processing,
                // catch the exception and return an HTTP 500 Internal Server Error response.
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}