using Microsoft.AspNetCore.Http;
using SportsWeek.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace SportsWeek.Controllers
{
    public class FixtureImagesController : ApiController
    {
        SportsWeekdbEntities db = new SportsWeekdbEntities();

        [HttpPost]
        public HttpResponseMessage UploadImage(int fixturesId)
        {
            try
            {
                // Get the files from the request (multiple files support)
                var httpRequest = HttpContext.Current.Request;
                var fixtureImages = httpRequest.Files;

                // Check if no files are uploaded
                if (fixtureImages.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No images uploaded.");
                }

                // Define valid image extensions
                var validExtensions = new List<string>
                                            {
                                                ".jpeg", ".jpg", ".png", ".webp", ".gif"
                                            };

                // Check if the fixture exists using your database context
                var fixtureExists = db.Fixtures.Any(f => f.id == fixturesId);
                if (!fixtureExists)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Fixture not found.");
                }

                // List to hold the image paths to return in the response
                var imagePaths = new List<string>();

                foreach (string fileKey in fixtureImages)
                {
                    var fixtureImage = fixtureImages[fileKey];

                    // Check if the file is valid
                    if (fixtureImage == null || fixtureImage.ContentLength == 0)
                    {
                        continue; // Skip invalid files
                    }

                    // Check if the file extension is valid
                    var extension = Path.GetExtension(fixtureImage.FileName).ToLower();
                    if (!validExtensions.Contains(extension))
                    {
                        continue; // Skip invalid files
                    }

                    // Generate a unique file name for each image
                    var fileName = Guid.NewGuid() + extension;

                    // Define the upload path for the image
                    var uploadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads", "fixtures");

                    // Ensure the directory exists
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Full file path to save the image
                    var filePath = Path.Combine(uploadPath, fileName);

                    // Save the image to the file system
                    fixtureImage.SaveAs(filePath);

                    // Create a record for the image in the FixturesImages table
                    var fixtureImageRecord = new FixturesImage
                    {
                        fixtures_id = fixturesId,
                        image_path = "/uploads/fixtures/" + fileName // Save relative path in the database
                    };

                    // Add the image record to the database and save changes
                    db.FixturesImages.Add(fixtureImageRecord);
                    db.SaveChanges();

                    // Add the image path to the list to return in the response
                    imagePaths.Add(fixtureImageRecord.image_path);
                }

                // Check if any images were successfully uploaded
                if (imagePaths.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No valid images uploaded.");
                }

                // Return the image paths as a response
                return Request.CreateResponse(HttpStatusCode.OK, imagePaths);
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetImages(int fixturesId)
        {
            try
            {
                // Retrieve all image records based on the fixtureId
                var fixtureImageRecords = db.FixturesImages.Where(f => f.fixtures_id == fixturesId).ToList();

                if (fixtureImageRecords == null || !fixtureImageRecords.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No images found for the given fixture.");
                }

                // Create a list of image file paths
                var imagePaths = new List<string>();

                foreach (var fixtureImageRecord in fixtureImageRecords)
                {
                    // Get the full file path from the database (the relative path)
                    var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads", "fixtures", Path.GetFileName(fixtureImageRecord.image_path));

                    // Check if the file exists
                    if (!File.Exists(imagePath))
                    {
                        continue; // Skip this image if it doesn't exist
                    }

                    // Add the valid image path to the list
                    imagePaths.Add(imagePath);
                }

                // If no valid images were found
                if (!imagePaths.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No valid images found for the given fixture.");
                }

                // Return the image file paths as a response (you can modify this to return the files as well)
                var response = Request.CreateResponse(HttpStatusCode.OK, imagePaths);

                return response;
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }


    }
}