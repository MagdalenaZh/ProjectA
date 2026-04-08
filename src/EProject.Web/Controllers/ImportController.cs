using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;
using System.Security.Claims;
using System.Text.Json;
using EProject.Web.Entities;
using EProject.Web.Models;

namespace EProject.Web.Controllers
{
    [Authorize] // only logged-in users
    public class ImportController : Controller
    {
        private readonly AppDbContext _context;

        public ImportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? importFile)
        {
            // File validation
            if (importFile == null || importFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid JSON file.";
                return RedirectToAction("Index");
            }

            if (Path.GetExtension(importFile.FileName).ToLower() != ".json")
            {
                TempData["ErrorMessage"] = "Only .json files are allowed.";
                return RedirectToAction("Index");
            }

            // Read file
            string jsonData;
            using (var reader = new StreamReader(importFile.OpenReadStream()))
            {
                jsonData = await reader.ReadToEndAsync();
            }

            // JSON schema
            var schemaJson = @"{
              ""type"": ""array"",
              ""items"": {
                ""type"": ""object"",
                ""properties"": {
                  ""Title"": { ""type"": ""string"", ""minLength"": 3, ""maxLength"": 150 },
                  ""Description"": { ""type"": ""string"", ""minLength"": 10, ""maxLength"": 2000 },
                  ""Author"": { ""type"": ""string"", ""minLength"": 2, ""maxLength"": 100 },
                  ""ProgrammingLanguage"": { ""type"": ""string"", ""minLength"": 2, ""maxLength"": 50 },
                  ""Status"": { ""type"": ""string"", ""enum"": [""pending"", ""in-progress"", ""complete""] }
                },
                ""required"": [""Title"", ""Description"", ""Author"", ""ProgrammingLanguage"", ""Status""],
                ""additionalProperties"": false
              }
            }";

            try
            {
                // Validate JSON string against the schema
                var schema = await JsonSchema.FromJsonAsync(schemaJson);
                var validationErrors = schema.Validate(jsonData);

                if (validationErrors.Any())
                {
                    var errorMessages = validationErrors.Select(e => $"Error at {e.Path}: {e.Kind}").ToList();
                    ViewBag.ValidationErrors = errorMessages;
                    ViewBag.ErrorMessage = "The JSON file failed schema validation. See details below.";
                    return View("Index");
                }

                // Deserialize and save
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var importedProjects = JsonSerializer.Deserialize<List<ProjectImportDto>>(jsonData, options);

                if (importedProjects == null || !importedProjects.Any())
                {
                    TempData["ErrorMessage"] = "The file was valid but contained no projects.";
                    return RedirectToAction("Index");
                }

                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int successCount = 0;

                foreach (var dto in importedProjects)
                {
                    var project = new Project
                    {
                        Title = dto.Title,
                        Description = dto.Description,
                        Author = dto.Author,
                        ProgrammingLanguage = dto.ProgrammingLanguage,
                        Status = dto.Status,
                        UserAccountId = user.Id,
                        CompletedAt = dto.Status.ToLower() == "complete" ? DateTime.UtcNow : null
                    };

                    _context.Projects.Add(project);
                    successCount++;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully imported {successCount} projects from JSON.";
                return RedirectToAction("Index", "Projects");
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "The uploaded file is not a valid JSON document (syntax error).";
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred: " + ex.Message;
                return View("Index");
            }
        }
    }
}