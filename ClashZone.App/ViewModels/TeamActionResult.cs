namespace ClashZone.App.ViewModels
{
    public class TeamActionResult
    {
        public bool Success { get; set; }
        public bool IsError => !Success;
        public string Message { get; set; } = string.Empty;
    }
}
