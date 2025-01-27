using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UTB.OfficeCalendar.Application.Abstraction;
using UTB.OfficeCalendar.Application.ViewModels;

namespace UTB.OfficeCalendar.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAccountService _accountService;

        public HomeController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid)
            {
                bool isLogged = await _accountService.Login(loginVM);
                if (isLogged)
                {
                    return RedirectToAction("CalendarDay", "Calendar", new { area = "User" });
                }

                ViewData["LoginFailed"] = true;
            }

            return View("Index", loginVM);
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _accountService.Logout(); 
            return RedirectToAction("Index"); 
        }


    }
}
