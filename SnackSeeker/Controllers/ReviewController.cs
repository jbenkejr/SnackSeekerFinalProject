using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SnackSeeker.Models;

namespace SnackSeeker.Controllers
{
	[Authorize]
    public class ReviewController : Controller
    {
		//Declaring variables to be used later for our API's and our database.
        private readonly string _yelpKey;
        private readonly HttpClient _client;
        private readonly SnacksDbContext _context;

		//Injected our database context and the configuration for our API keys.
		public ReviewController(IConfiguration configuration, SnacksDbContext context)
		{
			//Telling our program where to find our APIs and then starting a new API call
			//with the necessary base address and Header.
			_yelpKey = configuration.GetSection("ApiKeys")["YelpApi"];
			_client = new HttpClient();
			_client.BaseAddress = new Uri("https://api.yelp.com/v3/");
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _yelpKey);
			_context = context;
		}

		//Method to add or update the Reviews in our Review List for each user.
		public async Task<IActionResult> ReviewCheck(string BusinessId)
		{
			//Checking to see if user is logged in and setting their User Id
			string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

			//Adding all of the logged in user's reviews from our database to a list
			var reviews = _context.Review.ToList();

			//Running our Yelp API to get a specific business based on the business' unique Id from Yelp
			var reviewResponse = await _client.GetAsync($"businesses/{BusinessId}");
			var results = await reviewResponse.Content.ReadAsAsync<RestaurantRoot>();

			//Executing a foreach loop to check each review that specific User has in our database
			foreach (var review in reviews)
			{
				if (review.RestaurantId == BusinessId)
				{
					//If the restaurant id from one of the reviews in our database matches the id
					//we are searching for, we return the ReviewChange view to update that review
					ReviewModel updateReview = new ReviewModel();
					updateReview.Restaurant = results;
					updateReview.UpdateRestaurantReview = review;
					return View("ReviewChange", updateReview);
				}
			}
			//If none of our user's reviews restaurant ids match our current Id, we return to the
			//ReviewAdd view which allows us to add a new review for that Restaurant
            return View("ReviewAdd", results);
        }

		//Creating a method to update a user's reviews, passing in all the parameters needed from a RestaurantRoot model
        public IActionResult ReviewChange(string change, string restaurantId, int reviewOfPrice, double reviewOfType1, double reviewOfType2, double reviewOfType3, double reviewOfRating, string restaurantName)
        {
			//Finding the current user and putting their reviews into a List
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            
			var reviewList = _context.Review.Where(x => x.UserId == userId).ToList();
            Review review = new Review();

			//Finding the review we want to adjust based on the Restaurant Id we passed in
            foreach (var currentReview in reviewList)
            {
                if (currentReview.RestaurantId == restaurantId)
                {
                    review = currentReview;

                }
            }

			//If Delete button is chosen, this delete the review from the database
            if (change == "Delete")
            {
                _context.Review.Remove(review);
            }

			//If Update button is chosen, this updates any changes the user made
            else if (change == "Update")
            {
                review.RestaurantName = restaurantName;
                review.RestaurantId = restaurantId;
                review.UserId = userId;
                review.ReviewOfPrice = reviewOfPrice;
                review.ReviewOfRating = reviewOfRating;
                review.ReviewOfType1 = reviewOfType1;
                review.ReviewOfType2 = reviewOfType2;
                review.ReviewOfType3 = reviewOfType3;
                _context.Entry(review).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _context.Review.Update(review);
            }
			//Saves any changes made in the database and sends the user back to the Home Page - Preference Index
            _context.SaveChanges();
            return RedirectToAction("PreferenceIndex", "Snack");
        }
        //Method for creating a new review
        public IActionResult ReviewAdd(Review newReview)
        {
            //If all the necessary properties are included, a new review is added to the database
            if (ModelState.IsValid)
            {
                _context.Review.Add(newReview);
                _context.SaveChanges();
                return RedirectToAction("PreferenceIndex", "Snack");
            }

            //If the user does not enter the correct data, they are returned to the ReviewCheck method
            //along with the Restaurant Id they were using
            return RedirectToAction("ReviewCheck", newReview.RestaurantId);
        }

		//Method to create a list of reviews for the specific user that is logged in and returning the view with that List
        public IActionResult ListReviews()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var reviews = _context.Review.Where(x => x.UserId == userId).ToList();
            return View(reviews);
        }
    }
}