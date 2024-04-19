using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Domain.Users;
using Libota.Data.Configuration;
using Libota.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Libota.Data.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly LibotaDbContext _dbContext;
        private readonly IMapper _mapper;

        public UserRepository(LibotaDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        public async Task<IEnumerable<User>> GetAll()
        {
            var userDataSet = await _dbContext.Users.AsNoTracking().ToListAsync();
            return userDataSet.Select(x => _mapper.Map<UserData, User>(x));
        }

        public async Task<User> Add([NotNull] User newUser)
        {
            var userData = _mapper.Map<User, UserData>(newUser);
            var result = await _dbContext.Users.AddAsync(userData);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<UserData, User>(result.Entity);
        }

        public Task<User> Update(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Delete(User user)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<User> SingleOrDefault(Predicate<User> matchesCondition)
        {
            var responseList = await _dbContext.Users.ToListAsync();
            var allUsers =  responseList.Select(x => _mapper.Map<UserData, User>(x));
            return allUsers.SingleOrDefault(x => matchesCondition(x));
        }
    }
}