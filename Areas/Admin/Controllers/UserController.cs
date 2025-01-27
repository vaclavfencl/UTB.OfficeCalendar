using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UTB.OfficeCalendar.Application.Abstraction;
using UTB.OfficeCalendar.Application.ViewModels;
using UTB.OfficeCalendar.Controllers;
using UTB.OfficeCalendar.Infrastructure.Database;
using UTB.OfficeCalendar.Infrastructure.Identity.Enum;

namespace UTB.OfficeCalendar.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly IAccountService _accountService;

        public UserController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListUser()
        {
            var users = await _accountService.GetAllUsers();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //Dát do kupy DeleteUser (pouze vyšší role nižší). 
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User ID cannot be null or empty.";
                return RedirectToAction(nameof(ListUser));
            }

            var result = await _accountService.DeleteUser(userId);

            if (result)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(ListUser));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, Roles newRole)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User ID cannot be null or empty.";
                return RedirectToAction(nameof(ListUser));
            }

            if (newRole == Roles.Admin)
            {
                TempData["ErrorMessage"] = "You cannot promote a user to Admin.";
                return RedirectToAction(nameof(ListUser));
            }

            var result = await _accountService.ChangeUserRole(userId, newRole);

            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "User role updated successfully."
                : "Failed to update user role.";

            return RedirectToAction(nameof(ListUser));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User ID cannot be null or empty.";
                return RedirectToAction(nameof(ListUser));
            }

            var user = await _accountService.GetUserById(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ListUser));
            }

            string newPassword = new string(user.UserName.Reverse().ToArray());
            var result = await _accountService.ResetUserPassword(userId, newPassword);

            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "User password has been reset successfully."
                : "Failed to reset user password.";

            return RedirectToAction(nameof(ListUser));
        }

        [HttpPost]
        //Dát do kupy Vytvoření uživatele.
        public async Task<IActionResult> CreateUser(RegisterViewModel registerVM)
        {

            if (!ModelState.IsValid)
            {
                return View(registerVM);
            }

            var existingUserByUsername = await _accountService.GetUserByUsername(registerVM.UserName);
            if (existingUserByUsername != null)
            {
                ModelState.AddModelError("UserName", "Toto uživatelské jméno je již obsazeno.");
            }

            var existingUserByEmail = await _accountService.GetUserByEmail(registerVM.Email);
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError("Email", "Tento e-mail je již používán.");
            }

            if (!ModelState.IsValid)
            {
                return View(registerVM);
            }

            string[] errors = await _accountService.Register(registerVM, Roles.User);

            if (errors == null || errors.Length == 0)
            {
                return RedirectToAction(nameof(ListUser), new { area = "Admin" });
            }

            ModelState.AddModelError("", string.Join(", ", errors));

            return View(registerVM);
        }


    }
}
