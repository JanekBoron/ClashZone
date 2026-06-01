using AutoMapper;
using ClashZone.App.Domains;
using ClashZone.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Config
{
    public class UserSubscriptionProfile : Profile
    {
        public UserSubscriptionProfile()
        {
            CreateMap<UserSubscription, UserSubscriptionDomain>();
        }
    }
}
