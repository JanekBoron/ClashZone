using AutoMapper;
using ClashZone.App.Domains;
using DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    public class MatchProfile : Profile
    {
        public MatchProfile()
        {
            CreateMap<Match, MatchDomain>().ReverseMap();
        }
    }
}
