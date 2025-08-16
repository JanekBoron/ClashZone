using Microsoft.AspNetCore.Http;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used for editing a user's profile.  Fields are bound
    /// to the edit form allowing the user to change their username,
    /// display name, email, profile picture and optionally their password.
    /// </summary>
    public class EditProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public IFormFile? ProfileImage { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}