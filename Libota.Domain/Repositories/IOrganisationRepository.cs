using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Shared;

namespace Domain.Repositories
{
    public interface IOrganisationRepository
    {
        Task<Organisation?> Add(Organisation organisation);
        Task<IList<Organisation>> GetAll();
        Task<Organisation> GetById(Guid id);
        Task<Organisation> Update(Organisation organisation);
        Task<bool> Remove(Organisation organisation);
    }
}