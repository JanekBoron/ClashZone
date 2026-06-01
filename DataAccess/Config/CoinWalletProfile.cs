using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    public class CoinWalletProfile : Profile
    {
        public CoinWalletProfile()
        {
            CreateMap<CoinWallet, CoinWalletDomain>();
        }
    }
}
