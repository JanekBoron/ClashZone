
using ClashZone.App.Domains;

namespace ClashZone.App.ViewModels
{
    public class PlayerStatViewModel
    {
        public ClashUserDomain Player { get; set; } = null!;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string? Champion { get; set; }
        public double KDRatio => Deaths == 0 ? Kills : (double)Kills / Deaths;
        public double KDA => (double)(Kills + Assists) / (Deaths == 0 ? 1 : Deaths);
    }
}