using System.ComponentModel.DataAnnotations;

namespace ClashZone.App.ViewModels
{
    public class CheckoutViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int ClashCoins { get; set; }
        public string? ImageUrl { get; set; }
        public int Balance { get; set; }

        [Required(ErrorMessage = "Podaj imię i nazwisko.")]
        [StringLength(120)]
        [Display(Name = "Imię i nazwisko")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj numer telefonu.")]
        [StringLength(30)]
        [Phone(ErrorMessage = "Nieprawidłowy numer telefonu.")]
        [Display(Name = "Telefon")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj ulicę i numer.")]
        [StringLength(160)]
        [Display(Name = "Ulica i numer")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj kod pocztowy.")]
        [StringLength(12)]
        [Display(Name = "Kod pocztowy")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj miasto.")]
        [StringLength(80)]
        [Display(Name = "Miasto")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj kraj.")]
        [StringLength(80)]
        [Display(Name = "Kraj")]
        public string Country { get; set; } = "Polska";
    }
}
