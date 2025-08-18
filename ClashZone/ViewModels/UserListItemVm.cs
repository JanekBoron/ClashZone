using System.Collections.Generic;

namespace ClashZone.Web.ViewModels.Admin
{
    public class UserListItemVm
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsBanned { get; set; }
    }
}
