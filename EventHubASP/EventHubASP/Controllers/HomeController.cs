using EventHubASP.Core;
using EventHubASP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EventHubASP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFeedbackService _feedbackService;

        public HomeController(ILogger<HomeController> logger, IFeedbackService feedbackService)
        {
            _logger = logger;
            _feedbackService = feedbackService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ClearCookieNoticeFlag()
        {
            HttpContext.Session.Remove("ShowCookieNotice");
            return Ok();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ContactUs()
        {
            return View(new Feedback());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(Feedback feedback)
        {
            if (!ModelState.IsValid)
            {
                return View(feedback);
            }

            try
            {
                await _feedbackService.SubmitFeedbackAsync(feedback);
                TempData["SuccessMessage"] = "Feedback submitted successfully!";
                return RedirectToAction(nameof(ContactUs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                ModelState.AddModelError("", "An error occurred while submitting your feedback. Please try again.");
                return View(feedback);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
