using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;

namespace ClashZone.DataAccess.Config
{
    public class CoinWalletTransactionProfile : Profile
    {
        public CoinWalletTransactionProfile()
        {
            CreateMap<CoinWalletTransaction, CoinWalletTransactionDomain>().ReverseMap();
        }
    }
}
