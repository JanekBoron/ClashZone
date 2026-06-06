using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    /// <summary>
    /// Mapowania Entity↔Domain dla funkcjonalności ligi. Rejestrowane
    /// automatycznie przez skan assembly w <c>Program.cs</c>.
    /// </summary>
    public class LeagueProfile : Profile
    {
        public LeagueProfile()
        {
            CreateMap<LeagueTeam, LeagueTeamDomain>().ReverseMap();
            CreateMap<LeagueTeamMember, LeagueTeamMemberDomain>().ReverseMap();
            CreateMap<Season, SeasonDomain>().ReverseMap();
            CreateMap<LeagueRegistration, LeagueRegistrationDomain>().ReverseMap();
            CreateMap<LeagueMatch, LeagueMatchDomain>().ReverseMap();
        }
    }
}
