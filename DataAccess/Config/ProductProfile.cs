using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;


namespace ClashZone.DataAccess.Config
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDomain>().ReverseMap();
        }
    }
}
