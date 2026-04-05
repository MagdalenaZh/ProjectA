using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EProject.Web.Entities;
using EProject.Web.Models; 
using System.Linq;

namespace EProject.Web.Controllers
{
    [Authorize] // logged-in users only
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return View(new List<Project>());
            }

            var query = searchString.ToLower();

            // Get projects for occurrence count
            var allProjects = await _context.Projects.ToListAsync();

            var searchResults = allProjects
                .Select(p => new
                {
                    Project = p,
                    Score = CalculateScore(p, query)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Project)
                .ToList();

            ViewBag.SearchTerm = searchString;
            return View(searchResults);
        }

        private int CalculateScore(Project p, string term)
        {
            int Count(string source)
            {
                if (string.IsNullOrEmpty(source)) return 0;
                source = source.ToLower();
                return (source.Length - source.Replace(term, "").Length) / term.Length;
            }

            return Count(p.Title) +
                   Count(p.Description) +
                   Count(p.Author) +
                   Count(p.ProgrammingLanguage);
        }
    }
}