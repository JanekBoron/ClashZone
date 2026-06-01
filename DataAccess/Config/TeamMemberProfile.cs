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
    public class TeamMemberProfile : Profile
    {
        public TeamMemberProfile()
        {
                CreateMap<TeamMember, TeamMemberDomain>();
        }
    }
}
