using System.ComponentModel.DataAnnotations;

namespace EProject.Web.Entities
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ProgrammingLanguage { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int UserAccountId { get; set; }
    }
}