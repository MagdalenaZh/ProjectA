using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EProject.Web.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d\.]+$",
            ErrorMessage = "Password must contain letters, numbers, and dots only.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[A-Za-z\s'-]+$",
            ErrorMessage = "Name can contain only letters, spaces, apostrophes, and hyphens.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^\+?[0-9\s\-]{7,15}$",
            ErrorMessage = "Please enter a valid phone number.")]
        public string Phone { get; set; } = string.Empty;
    }
}