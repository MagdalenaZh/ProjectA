using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EProject.Web.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[A-Za-z\s'-]+$",
            ErrorMessage = "Name can contain only letters, spaces, apostrophes, and hyphens.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?[0-9\s-]{7,15}$",
            ErrorMessage = "Please enter a valid phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Regex.IsMatch(Password, "[A-Za-z]"))
            {
                yield return new ValidationResult(
                    "Password must contain at least one letter.",
                    new[] { nameof(Password) });
            }

            if (!Regex.IsMatch(Password, "[0-9]"))
            {
                yield return new ValidationResult(
                    "Password must contain at least one number.",
                    new[] { nameof(Password) });
            }

            if (!Regex.IsMatch(Password, @"^[A-Za-z0-9.]+$"))
            {
                yield return new ValidationResult(
                    "Password can only contain letters, numbers, and dots.",
                    new[] { nameof(Password) });
            }
        }
    }
}

