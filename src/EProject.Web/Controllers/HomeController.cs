using EProject.Web.Entities;
using EProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace EProject.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        int? currentUserId = null;

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    currentUserId = user.Id;
                }
            }
        }

        var completedProjects = await _context.Projects
            .Where(p => p.Status == "complete")
            .OrderByDescending(p => p.CompletedAt)
            .ThenByDescending(p => p.Id)
            .Take(5)
            .ToListAsync();

        ViewBag.CurrentUserId = currentUserId;

        return View(completedProjects);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}