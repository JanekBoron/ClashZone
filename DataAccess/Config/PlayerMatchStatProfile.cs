using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;


namespace ClashZone.DataAccess.Config
{
    public class PlayerMatchStatProfile : Profile
    {
        public PlayerMatchStatProfile()
        {
            CreateMap<PlayerMatchStat, PlayerMatchStatDomain>().ReverseMap();
        }
    }
}
