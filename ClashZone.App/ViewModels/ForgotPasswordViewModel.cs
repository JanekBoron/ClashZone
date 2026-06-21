using System.ComponentModel.DataAnnotations;

namespace ClashZone.App.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}