using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Organisation.Repositories;

public interface IOrganisationRepository
{
    Task<IList<Entities.Organisation>> GetAll();
    Task<Entities.Organisation> GetById(Guid id);
}