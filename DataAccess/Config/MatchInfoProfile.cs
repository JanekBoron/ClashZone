using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    public class MatchInfoProfile : Profile
    {
        public MatchInfoProfile()
        {
            CreateMap<MatchInfo, MatchInfoDomain>();
        }
    }
}
