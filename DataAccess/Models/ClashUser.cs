using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Models
{
    public class ClashUser : IdentityUser
    {
        public string? DisplayName { get; set; }

        public int Score { get; set; }

        public string? Awards { get; set; }

        public string? ProfilePicturePath { get; set; }

        [Required]
        public bool IsBanned { get; set; } = false;

        public string? RiotId { get; set; }

        public string? RiotPuuid { get; set; }
    }
}