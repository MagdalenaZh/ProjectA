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
        // Get 5 most-recently completed projects for all
        var completedProjects = await _context.Projects
            .Where(p => p.Status == "complete")
            .OrderByDescending(p => p.Id)
            .Take(5)
            .ToListAsync();

        var userProjects = new List<EProject.Web.Entities.Project>();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    // Get the specific user's projects 
                    userProjects = await _context.Projects
                        .Where(p => p.UserAccountId == user.Id)
                        .OrderByDescending(p => p.Id)
                        .ToListAsync();
                }
            }
        }

        // Mix both lists so the View displays all relevant projects
        var allProjects = completedProjects.Union(userProjects).ToList();

        return View(allProjects);
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