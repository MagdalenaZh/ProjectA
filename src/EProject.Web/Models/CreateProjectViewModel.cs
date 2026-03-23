using System.ComponentModel.DataAnnotations;

namespace EProject.Web.Models
{
    public class CreateProjectViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Author must be between 2 and 100 characters.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Programming language is required.")]
        [Display(Name = "Programming Language")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Programming language must be between 2 and 50 characters.")]
        public string ProgrammingLanguage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(pending|in-progress|complete)$", ErrorMessage = "Please select a valid status.")]
        public string Status { get; set; } = string.Empty;
    }
}