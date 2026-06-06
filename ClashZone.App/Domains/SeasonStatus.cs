namespace ClashZone.App.Domains
{
    /// <summary>
    /// Fazy sezonu ligi. Wartości liczbowe są utrwalane w kolumnie
    /// <c>Season.Status</c>.
    /// </summary>
    public enum SeasonStatus
    {
        Registration = 0,
        RegularSeason = 1,
        Playoffs = 2,
        Completed = 3
    }
}
