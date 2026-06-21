using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;


namespace ClashZone.DataAccess.Config
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDomain>()
                .ReverseMap()
                .ForMember(d => d.Category, opt => opt.Ignore())
                .ForMember(d => d.Images, opt => opt.Ignore());
        }
    }
}
