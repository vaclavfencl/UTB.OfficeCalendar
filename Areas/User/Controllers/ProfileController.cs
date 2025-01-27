using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UTB.OfficeCalendar.Application.Abstraction;
using UTB.OfficeCalendar.Application.ViewModels;

namespace UTB.OfficeCalendar.Areas.User.Controllers
{
    [Area("User")]
    public class ProfileController : Controller
    {
        private readonly IAccountService _userService;
        private readonly IFileUploadService _fileUploadService;

        public ProfileController(IAccountService userService, IFileUploadService fileUploadService)
        {
            _userService = userService;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var viewModel = new ProfileViewModel
            {
                FirstName = _userService.GetFirstName(userId),
                LastName = _userService.GetLastName(userId),
                Role = await _userService.GetRole(userId), // ✅ Await the async method
                Email = _userService.GetEmail(userId),
                PhoneNumber = _userService.GetPhoneNumber(userId),
                ProfilePicturePath = await _userService.GetProfilePicturePath(userId) // ✅ Get Profile Picture Path
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult SaveProfile(ProfileViewModel model, IFormFile? Image)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (ModelState.IsValid)
            {
                if (Image != null && Image.Length > 0)
                {
                    string folderName = "img/profile_pictures";
                    string imageSrc = _fileUploadService.FileUpload(Image, folderName);
                    _userService.UpdateProfilePicture(userId, imageSrc);
                }

                bool updated = _userService.UpdateUserProfile(userId, model.Email, model.PhoneNumber, model.Password);
                if (updated)
                {
                    TempData["Success"] = "Úspěšně uloženo!";
                }
                else
                {
                    TempData["Error"] = "Chyba při ukládání.";
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public IActionResult DeleteProfilePicture()
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            bool isDeleted = _userService.UpdateProfilePicture(userId, "/img/default-profile.jpg");

            if (isDeleted)
            {
                TempData["Success"] = "Profilový obrázek byl odstraněn!";
            }
            else
            {
                TempData["Error"] = "Chyba při odstraňování obrázku.";
            }

            return RedirectToAction("Profile");
        }
    }
}
