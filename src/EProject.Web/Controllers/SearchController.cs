using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EProject.Web.Entities;
using System.Security.Claims;

namespace EProject.Web.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchString)
        {
            ViewBag.SearchTerm = searchString;

            if (string.IsNullOrWhiteSpace(searchString))
            {
                return View("~/Views/Projects/Search.cshtml", new List<Project>());
            }

            var email = User.FindFirstValue(ClaimTypes.Email);
            var currentUser = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);
            ViewBag.CurrentUserId = currentUser?.Id;

            var term = searchString.Trim().ToLower();

            var allProjects = await _context.Projects
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var results = allProjects
                .Select(p => new
                {
                    Project = p,
                    Score = CalculateScore(p, term)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Project.Id)
                .Select(x => x.Project)
                .ToList();

            return View("~/Views/Projects/Search.cshtml", results);
        }

        private static int CalculateScore(Project p, string term)
        {
            static int CountOccurrences(string? source, string term)
            {
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(term))
                    return 0;

                source = source.ToLower();

                int count = 0;
                int index = 0;

                while ((index = source.IndexOf(term, index, StringComparison.Ordinal)) != -1)
                {
                    count++;
                    index += term.Length;
                }

                return count;
            }

            return CountOccurrences(p.Title, term)
                 + CountOccurrences(p.Description, term)
                 + CountOccurrences(p.Author, term)
                 + CountOccurrences(p.ProgrammingLanguage, term);
        }
    }
}