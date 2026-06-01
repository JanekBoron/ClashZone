using AutoMapper;
using ClashZone.App.Domains;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Config
{
    public class TournamentProfile: Profile
    {
        public TournamentProfile()
        {
            CreateMap<Tournament, TournamentDomain>().ReverseMap();
        }
    }
}
