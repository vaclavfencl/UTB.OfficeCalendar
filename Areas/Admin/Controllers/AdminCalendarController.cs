using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UTB.OfficeCalendar.Application.Abstraction;
using UTB.OfficeCalendar.Domain.Entities;
using UTB.OfficeCalendar.Infrastructure.Database;

namespace UTB.OfficeCalendar.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminCalendarController : Controller
    {
        private readonly IAccountService _userService;
        private readonly CalendarDbContext _context;

        public AdminCalendarController(CalendarDbContext context, IAccountService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> AdminMonth()
        {
            var requests = await _context.Requests
                .Where(r => !r.Accepted)
                .ToListAsync(); // Fetch data first

            var requestsWithNames = new List<dynamic>();

            foreach (var request in requests)
            {
                var fullName = await _userService.GetName(request.UserId);
                requestsWithNames.Add(new
                {
                    request.Id,
                    request.RequestDate,
                    FullName = fullName
                });
            }

            // Sort by RequestDate first, then alphabetically by FullName
            var sortedRequests = requestsWithNames
                .OrderBy(r => r.RequestDate)
                .ThenBy(r => r.FullName)
                .ToList();

            return View(sortedRequests);
        }



        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                request.Accepted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AdminMonth");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("AdminMonth");
        }
    }
}
