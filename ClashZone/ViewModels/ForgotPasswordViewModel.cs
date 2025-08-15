using System.ComponentModel.DataAnnotations;

namespace ClashZone.ViewModels
{
    /// <summary>
    /// View model used when requesting a password reset.  The user must provide
    /// the email address associated with their account.  Data annotations
    /// enforce that the email is present and in a valid format.
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}