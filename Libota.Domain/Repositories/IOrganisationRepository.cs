using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Shared;

namespace Domain.Repositories
{
    public interface IOrganisationRepository
    {
        Task<IList<Organisation>> GetAll();
        Task<Organisation> GetById(Guid id);
    }
}