using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Entities.Shared;
using Domain.Repositories;
using Libota.Data.Configuration;
using Libota.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Repositories
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private readonly LibotaDbContext _dbContext;
        private readonly IMapper _mapper;

        public OrganisationRepository(LibotaDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<Organisation> Add(Organisation organisation)
        {
            var dto = _mapper.Map<Organisation, OrganisationData>(organisation);
            var response = await _dbContext.Organisations.AddAsync(dto);
            var newOrganisation = _mapper.Map<OrganisationData, Organisation>(response.Entity);
            return newOrganisation;
        }

        public async Task<IList<Organisation>> GetAll()
        {
            var dtos = await _dbContext.Organisations.ToListAsync();
            return dtos.Select(x => _mapper.Map<OrganisationData, Organisation>(x)).ToList();
        }

        public Task<Organisation> GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Organisation> Update(Organisation organisation)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Remove(Organisation organisation)
        {
            throw new NotImplementedException();
        }
    }
}