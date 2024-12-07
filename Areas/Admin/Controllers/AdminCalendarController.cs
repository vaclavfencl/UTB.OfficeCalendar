using Microsoft.AspNetCore.Mvc;

namespace UTB.OfficeCalendar.Areas.User.Controllers
{
    [Area("Admin")] // Specifies that this controller belongs to the "User" area
    public class AdminCalendarController : Controller
    {
        public IActionResult AdminShared()
        {
            return View(); // Looks for CalendarDay.cshtml in Areas/User/Views/Calendar/
        }

        public IActionResult AdminMonth()
        {
            return View();
        }
    }
}
