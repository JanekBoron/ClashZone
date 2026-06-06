using System;
using System.ComponentModel.DataAnnotations;

namespace ClashZone.App.ViewModels
{
    /// <summary>Formularz tworzenia/edycji sezonu ligi (admin/organizator).</summary>
    public class SeasonFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Game { get; set; } = string.Empty;

        [Required]
        public string Format { get; set; } = "5v5";

        public DateTime RegistrationOpenDate { get; set; } = DateTime.UtcNow;
        public DateTime RegistrationCloseDate { get; set; } = DateTime.UtcNow.AddDays(7);
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(10);

        [Range(1, 10)]
        public int RosterSize { get; set; } = 5;

        [Range(2, 32)]
        public int PlayoffSpots { get; set; } = 4;

        public string FirstPlacePrize { get; set; } = string.Empty;
        public string SecondPlacePrize { get; set; } = string.Empty;
        public string ThirdPlacePrize { get; set; } = string.Empty;

        public string? DefaultMatchDay { get; set; }
        public string? Rules { get; set; }
    }
}
