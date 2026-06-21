
using Microsoft.AspNetCore.Identity;

namespace ClashZone.App.Domains
{
    public class ClashUserDomain : IdentityUser
    {
        public string? DisplayName { get; set; }
        public int Score { get; set; }
        public string? Awards { get; set; }
        public string? ProfilePicturePath { get; set; }
        public bool IsBanned { get; set; } = false;
        public string? RiotId { get; set; }
        public string? RiotPuuid { get; set; }
    }
}
