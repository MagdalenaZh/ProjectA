using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EProject.Web.Entities;
using EProject.Web.Models;

namespace EProject.Web.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        private static bool IsCompleteStatus(string? status)
        {
            return string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var projects = await _context.Projects
                .Where(p => p.UserAccountId == user.Id)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(projects);
        }

        [HttpGet]
        public IActionResult Create(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new CreateProjectViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User account not found.");
                return View(model);
            }

            try
            {
                var project = new Project
                {
                    Title = model.Title,
                    Description = model.Description,
                    Author = model.Author,
                    ProgrammingLanguage = model.ProgrammingLanguage,
                    Status = model.Status,
                    CompletedAt = IsCompleteStatus(model.Status) ? DateTime.UtcNow : null,
                    UserAccountId = user.Id
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project added successfully.";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "An error occurred while saving the project.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserAccountId == user.Id);

            if (project == null)
            {
                return NotFound();
            }

            var model = new EditProjectViewModel
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                Author = project.Author,
                ProgrammingLanguage = project.ProgrammingLanguage,
                Status = project.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.UserAccountId == user.Id);

            if (project == null)
            {
                return NotFound();
            }

            try
            {
                var wasComplete = IsCompleteStatus(project.Status);
                var isNowComplete = IsCompleteStatus(model.Status);

                project.Title = model.Title;
                project.Description = model.Description;
                project.Author = model.Author;
                project.ProgrammingLanguage = model.ProgrammingLanguage;
                project.Status = model.Status;

                if (!wasComplete && isNowComplete)
                {
                    project.CompletedAt = DateTime.UtcNow;
                }
                else if (wasComplete && !isNowComplete)
                {
                    project.CompletedAt = null;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project updated successfully.";
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating the project.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserAccountId == user.Id);

            if (project == null)
            {
                return NotFound();
            }

            try
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Project deleted successfully.";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the project.";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index");
            }
        }
    }
}