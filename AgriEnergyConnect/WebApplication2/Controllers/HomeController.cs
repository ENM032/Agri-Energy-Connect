using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                _logger.LogInformation("Home page accessed by user {UserId} from IP {IPAddress}", 
                    User?.Identity?.Name ?? "Anonymous", 
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public IActionResult Privacy()
        {
            try
            {
                _logger.LogInformation("Privacy page accessed by user {UserId}", User?.Identity?.Name ?? "Anonymous");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading privacy page");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Error page displayed with RequestId: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
