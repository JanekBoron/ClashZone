using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.App.ViewModels;

namespace ClashZone.DataAccess.Config
{
    public class TournamentFormProfile : Profile
    {
        public TournamentFormProfile()
        {
            CreateMap<TournamentFormViewModel, TournamentDomain>();
            CreateMap<TournamentDomain, TournamentFormViewModel>();
        }
    }
}
