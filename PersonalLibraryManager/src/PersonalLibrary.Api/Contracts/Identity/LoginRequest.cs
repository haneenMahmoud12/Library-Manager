using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Identity
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
    }
}
