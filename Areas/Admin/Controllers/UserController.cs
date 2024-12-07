using Microsoft.AspNetCore.Mvc;

namespace UTB.OfficeCalendar.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        public IActionResult CreateUser()
        {
            return View();
        }
    }
}
