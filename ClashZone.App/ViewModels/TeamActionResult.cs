namespace ClashZone.App.ViewModels
{
    /// <summary>
    /// Wynik akcji na drużynie turniejowej (gotowość / potwierdzenie). Niesie
    /// komunikat do wyświetlenia użytkownikowi oraz informację, czy to błąd.
    /// </summary>
    public class TeamActionResult
    {
        public bool Success { get; set; }
        public bool IsError => !Success;
        public string Message { get; set; } = string.Empty;
    }
}
