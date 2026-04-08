using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;
using System.Security.Claims;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using EProject.Web.Entities;
using EProject.Web.Models;

namespace EProject.Web.Controllers
{
    [Authorize]
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
            return View("~/Views/Projects/Import.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? importFile)
        {
            if (importFile == null || importFile.Length == 0)
            {
                ViewBag.ErrorMessage = "Please select a JSON or XML file to upload.";
                return View("~/Views/Projects/Import.cshtml");
            }

            var extension = Path.GetExtension(importFile.FileName).ToLowerInvariant();

            if (extension != ".json" && extension != ".xml")
            {
                ViewBag.ErrorMessage = "Only .json and .xml files are allowed.";
                return View("~/Views/Projects/Import.cshtml");
            }

            string fileData;
            using (var reader = new StreamReader(importFile.OpenReadStream()))
            {
                fileData = await reader.ReadToEndAsync();
            }

            try
            {
                List<ProjectImportDto> importedProjects;

                if (extension == ".json")
                {
                    importedProjects = await ParseAndValidateJsonAsync(fileData);
                }
                else
                {
                    importedProjects = ParseAndValidateXml(fileData);
                }

                if (importedProjects == null || !importedProjects.Any())
                {
                    ViewBag.ErrorMessage = "The file is valid, but it does not contain any projects.";
                    return View("~/Views/Projects/Import.cshtml");
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
                    var normalizedStatus = dto.Status.Trim().ToLowerInvariant();

                    var project = new Project
                    {
                        Title = dto.Title.Trim(),
                        Description = dto.Description.Trim(),
                        Author = dto.Author.Trim(),
                        ProgrammingLanguage = dto.ProgrammingLanguage.Trim(),
                        Status = normalizedStatus,
                        UserAccountId = user.Id,
                        CompletedAt = normalizedStatus == "complete" ? DateTime.UtcNow : null
                    };

                    _context.Projects.Add(project);
                    successCount++;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully imported {successCount} project(s).";
                return RedirectToAction("Index");
            }
            catch (ImportValidationException ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                ViewBag.ValidationErrors = ex.Errors;
                return View("~/Views/Projects/Import.cshtml");
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "The uploaded JSON file is not valid JSON syntax.";
                return View("~/Views/Projects/Import.cshtml");
            }
            catch (XmlException)
            {
                ViewBag.ErrorMessage = "The uploaded XML file is not valid XML syntax.";
                return View("~/Views/Projects/Import.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An unexpected error occurred while importing the file.";
                ViewBag.ValidationErrors = new List<string> { ex.Message };
                return View("~/Views/Projects/Import.cshtml");
            }
        }

        private async Task<List<ProjectImportDto>> ParseAndValidateJsonAsync(string jsonData)
        {
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

            var schema = await JsonSchema.FromJsonAsync(schemaJson);
            var errors = schema.Validate(jsonData);

            if (errors.Any())
            {
                throw new ImportValidationException(
                    "The JSON file failed schema validation.",
                    errors.Select(e =>
                        string.IsNullOrWhiteSpace(e.Path)
                            ? e.Kind.ToString()
                            : $"{e.Path}: {e.Kind}")
                    .ToList()
                );
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var importedProjects = JsonSerializer.Deserialize<List<ProjectImportDto>>(jsonData, options);

            if (importedProjects == null)
            {
                throw new ImportValidationException("The JSON file could not be read as a list of projects.");
            }

            return importedProjects;
        }

        private List<ProjectImportDto> ParseAndValidateXml(string xmlData)
        {
            var dtd = @"<!DOCTYPE projects [
<!ELEMENT projects (project+)>
<!ELEMENT project (Title, Description, Author, ProgrammingLanguage, Status)>
<!ELEMENT Title (#PCDATA)>
<!ELEMENT Description (#PCDATA)>
<!ELEMENT Author (#PCDATA)>
<!ELEMENT ProgrammingLanguage (#PCDATA)>
<!ELEMENT Status (#PCDATA)>
]>";

            string xmlWithDtd;

            if (xmlData.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            {
                xmlWithDtd = xmlData;
            }
            else if (xmlData.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                var endOfDeclaration = xmlData.IndexOf("?>", StringComparison.Ordinal);
                if (endOfDeclaration >= 0)
                {
                    xmlWithDtd = xmlData.Insert(endOfDeclaration + 2, Environment.NewLine + dtd + Environment.NewLine);
                }
                else
                {
                    xmlWithDtd = dtd + Environment.NewLine + xmlData;
                }
            }
            else
            {
                xmlWithDtd = dtd + Environment.NewLine + xmlData;
            }

            var validationErrors = new List<string>();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                ValidationType = ValidationType.DTD
            };

            settings.ValidationEventHandler += (sender, args) =>
            {
                validationErrors.Add(args.Message);
            };

            XDocument document;
            using (var stringReader = new StringReader(xmlWithDtd))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                document = XDocument.Load(xmlReader);
            }

            if (validationErrors.Any())
            {
                throw new ImportValidationException("The XML file failed DTD validation.", validationErrors);
            }

            if (document.Root == null || document.Root.Name != "projects")
            {
                throw new ImportValidationException("The XML root element must be <projects>.");
            }

            var projects = document.Root.Elements("project")
                .Select(x => new ProjectImportDto
                {
                    Title = (string?)x.Element("Title") ?? string.Empty,
                    Description = (string?)x.Element("Description") ?? string.Empty,
                    Author = (string?)x.Element("Author") ?? string.Empty,
                    ProgrammingLanguage = (string?)x.Element("ProgrammingLanguage") ?? string.Empty,
                    Status = (string?)x.Element("Status") ?? string.Empty
                })
                .ToList();

            var manualErrors = new List<string>();

            foreach (var project in projects.Select((value, index) => new { value, index }))
            {
                var item = project.value;
                int number = project.index + 1;

                if (item.Title.Length < 3 || item.Title.Length > 150)
                    manualErrors.Add($"Project {number}: Title must be between 3 and 150 characters.");

                if (item.Description.Length < 10 || item.Description.Length > 2000)
                    manualErrors.Add($"Project {number}: Description must be between 10 and 2000 characters.");

                if (item.Author.Length < 2 || item.Author.Length > 100)
                    manualErrors.Add($"Project {number}: Author must be between 2 and 100 characters.");

                if (item.ProgrammingLanguage.Length < 2 || item.ProgrammingLanguage.Length > 50)
                    manualErrors.Add($"Project {number}: ProgrammingLanguage must be between 2 and 50 characters.");

                if (item.Status != "pending" && item.Status != "in-progress" && item.Status != "complete")
                    manualErrors.Add($"Project {number}: Status must be pending, in-progress, or complete.");
            }

            if (manualErrors.Any())
            {
                throw new ImportValidationException("The XML file contains invalid project data.", manualErrors);
            }

            return projects;
        }

        private class ImportValidationException : Exception
        {
            public List<string> Errors { get; }

            public ImportValidationException(string message, List<string>? errors = null)
                : base(message)
            {
                Errors = errors ?? new List<string>();
            }
        }
    }
}