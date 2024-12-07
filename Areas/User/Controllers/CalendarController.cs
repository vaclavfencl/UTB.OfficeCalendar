using Microsoft.AspNetCore.Mvc;

namespace UTB.OfficeCalendar.Areas.User.Controllers
{
    [Area("User")] // Specifies that this controller belongs to the "User" area
    public class CalendarController : Controller
    {
        public IActionResult CalendarDay()
        {
            return View(); // Looks for CalendarDay.cshtml in Areas/User/Views/Calendar/
        }

        public IActionResult CalendarMonth()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult SharedCalendar()
        {
            return View();
        }
    }
}
