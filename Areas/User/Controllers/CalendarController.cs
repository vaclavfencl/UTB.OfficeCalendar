using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using UTB.OfficeCalendar.Domain.Entities;
using UTB.OfficeCalendar.Infrastructure.Database;
using UTB.OfficeCalendar.Application.ViewModels;

namespace UTB.OfficeCalendar.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly CalendarDbContext _context;

        public CalendarController(CalendarDbContext context)
        {
            _context = context;
        }

        public IActionResult CalendarDay(DateTime? date)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var selectedDate = date ?? DateTime.Today;

            var events = _context.Events
                .Where(e => e.UserId == userId && e.DateOfJob.Date == selectedDate.Date)
                .OrderBy(e => e.TimeStart)
                .Select(e => new EventViewModel
                {
                    Title = e.Title,
                    Description = e.Description,
                    TimeStart = e.TimeStart,
                    TimeEnd = e.TimeEnd
                })
                .ToList();

            var viewModel = new CalendarViewModel
            {
                CurrentDate = selectedDate,
                Events = events
            };

            return View(viewModel);
        }


        //Admin view
        public IActionResult SharedCalendar(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var userRole = User.IsInRole("Admin") || User.IsInRole("Manager") ? "Admin" : "User";

            var sharedCalendarViewModel = new CalendarViewModel
            {
                CurrentDate = selectedDate,
                SharedUserEvents = GetUserEventsForDate(selectedDate)
            };

            ViewData["UserRole"] = userRole; // Pass role to view
            return View(sharedCalendarViewModel);
        }


        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEvent([FromForm] EventViewModel model, [FromForm] int UserId)
        {
            if (model.TimeStart >= model.TimeEnd)
                return BadRequest("End time must be later than start time.");

            var user = _context.Users.FirstOrDefault(u => u.Id == UserId);
            if (user == null) return BadRequest("Invalid user.");

            // Check for overlapping events
            bool isOverlap = _context.Events.Any(e =>
                e.UserId == UserId &&
                e.DateOfJob.Date == model.DateOfJob.Date &&
                ((model.TimeStart >= e.TimeStart && model.TimeStart < e.TimeEnd) ||
                 (model.TimeEnd > e.TimeStart && model.TimeEnd <= e.TimeEnd) ||
                 (model.TimeStart <= e.TimeStart && model.TimeEnd >= e.TimeEnd))
            );

            if (isOverlap)
                return BadRequest("Another event already exists in this time slot.");

            var newEvent = new Event
            {
                Title = model.Title,
                Description = model.Description,
                UserId = user.Id,
                DateOfJob = model.DateOfJob,
                TimeStart = model.TimeStart,
                TimeEnd = model.TimeEnd
            };

            _context.Events.Add(newEvent);
            _context.SaveChanges();
            return Ok();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateRequest(DateTime selectedDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0 || selectedDate == default)
            {
                return RedirectToAction("CalendarMonth");
            }

            var request = new Request
            {
                UserId = userId,
                RequestDate = selectedDate,
                Accepted = false
            };

            _context.Requests.Add(request);
            _context.SaveChanges();

            return RedirectToAction("CalendarMonth");
        }

        public IActionResult CalendarMonth()
        {
            return View();
        }

        public IActionResult GetRequestsForMonth(int year, int month)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var requests = _context.Requests
                .Where(r => r.UserId == userId && r.RequestDate >= startDate && r.RequestDate <= endDate) // Filter by user ID
                .GroupBy(r => r.RequestDate.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Accepted = g.Any(r => r.Accepted),
                    Pending = g.Any(r => !r.Accepted),
                    RequestId = g.FirstOrDefault().Id
                })
                .ToList();

            return Json(requests);
        }


        [HttpDelete]
        [Authorize]
        public IActionResult DeleteRequest(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var request = _context.Requests.FirstOrDefault(r => r.Id == id && r.UserId == userId);
            if (request == null)
            {
                return NotFound();
            }

            _context.Requests.Remove(request);
            _context.SaveChanges();

            return Ok();
        }


        private List<SharedCalendarRowViewModel> GetUserEventsForDate(DateTime date)
        {
            var acceptedUserIds = _context.Requests
                .Where(r => r.Accepted && r.RequestDate.Date == date.Date)
                .Select(r => r.UserId)
                .Distinct()
                .ToList();

            var users = _context.Users
                .Where(u => acceptedUserIds.Contains(u.Id))
                .ToList();

            var userEvents = users.Select(user => new SharedCalendarRowViewModel
            {
                UserName = $"{user.FirstName} {user.LastName}" ?? "Unknown",
                Events = _context.Events
                    .Where(e => e.UserId == user.Id && e.DateOfJob.Date == date.Date)
                    .OrderBy(e => e.TimeStart)
                    .Select(e => new UTB.OfficeCalendar.Application.ViewModels.EventViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        TimeStart = e.TimeStart,
                        TimeEnd = e.TimeEnd
                    })
                    .ToList()
            }).ToList();

            return userEvents;
        }
        public IActionResult GetAvailableUsersForDate(DateTime date)
        {
            var acceptedUsers = _context.Requests
                .Where(r => r.Accepted && r.RequestDate.Date == date.Date)
                .Join(
                    _context.Users,  // Join with Users table
                    request => request.UserId,
                    user => user.Id,
                    (request, user) => new { UserId = user.Id, UserName = user.UserName }
                )
                .ToList();

            return Json(acceptedUsers);
        }

        [HttpDelete]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult DeleteEvent(int id)
        {
            var eventToDelete = _context.Events.FirstOrDefault(e => e.Id == id);

            if (eventToDelete == null)
                return NotFound("Event not found.");

            _context.Events.Remove(eventToDelete);
            _context.SaveChanges();

            return Ok();
        }
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult GetEvent(int id)
        {
            var eventDetails = _context.Events
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    DateOfJob = e.DateOfJob,
                    TimeStart = e.TimeStart,
                    TimeEnd = e.TimeEnd, 
                    UserId = e.UserId
                })
                .FirstOrDefault();

            if (eventDetails == null)
                return NotFound("Event not found.");

            return Json(eventDetails);
        }

        [HttpPut]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult EditEvent([FromForm] EventViewModel model)
        {
            var eventToUpdate = _context.Events.FirstOrDefault(e => e.Id == model.Id);

            if (eventToUpdate == null)
                return NotFound("Event not found.");

            if (model.TimeStart >= model.TimeEnd)
                return BadRequest("End time must be later than start time.");

            // Check for overlapping events excluding the current event being edited
            bool isOverlap = _context.Events.Any(e =>
                e.UserId == eventToUpdate.UserId &&
                e.DateOfJob.Date == model.DateOfJob.Date &&
                e.Id != model.Id &&  // Exclude the event being edited
                ((model.TimeStart >= e.TimeStart && model.TimeStart < e.TimeEnd) ||
                 (model.TimeEnd > e.TimeStart && model.TimeEnd <= e.TimeEnd) ||
                 (model.TimeStart <= e.TimeStart && model.TimeEnd >= e.TimeEnd))
            );

            if (isOverlap)
                return BadRequest("Another event already exists in this time slot.");

            eventToUpdate.Title = model.Title;
            eventToUpdate.Description = model.Description;
            eventToUpdate.DateOfJob = model.DateOfJob;
            eventToUpdate.TimeStart = model.TimeStart;
            eventToUpdate.TimeEnd = model.TimeEnd;

            if (Request.Form.TryGetValue("UserId", out var userIdString) && int.TryParse(userIdString, out int newUserId))
            {
                eventToUpdate.UserId = newUserId;
            }

            _context.SaveChanges();

            return Ok();
        }


    }
}
