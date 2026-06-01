using AutoMapper;
using ClashZone.DataAccess.Models;
using ClashZone.App.Domains;

namespace ClashZone.DataAccess.Config
{
    public class ChatMessageProfile : Profile
    {
        public ChatMessageProfile()
        {
            CreateMap<ChatMessage, ChatMessageDomain>().ReverseMap();
        }
    }
}
