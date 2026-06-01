using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    public class ClashUserProfile: Profile
    {
        public ClashUserProfile()
        {
            CreateMap<ClashUser, ClashUserDomain>();
        }
    }
}
