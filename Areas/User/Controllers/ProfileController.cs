using Microsoft.AspNetCore.Mvc;

namespace UTB.OfficeCalendar.Areas.User.Controllers
{
    [Area("User")] // Belongs to the User area
    public class ProfileController : Controller
    {
        public IActionResult Profile()
        {
            return View();
        }
    }
}
