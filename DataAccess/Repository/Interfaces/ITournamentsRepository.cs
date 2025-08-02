using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Models;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    public interface ITournamentsRepository
    {
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync();
    }
}
