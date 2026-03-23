using EProject.Web.Entities;
using EProject.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
        var projects = new List<EProject.Web.Entities.Project>();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var email = User.Identity.Name;

            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    projects = await _context.Projects
                        .Where(p => p.UserAccountId == user.Id)
                        .OrderByDescending(p => p.Id)
                        .ToListAsync();
                }
            }
        }

        return View(projects);
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