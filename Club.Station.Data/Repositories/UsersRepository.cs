using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Club.Station.Data.Configuration;
using Domain.Entities.Users;
using Domain.Repositories;

namespace Club.Station.Data.Repositories
{
    public class UsersRepository: IUserRepository
    {
        private readonly DefaultDbContext _dbContext;

        public UsersRepository(DefaultDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public Task<IEnumerable<User>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<User> Add(User newUser)
        {
            throw new NotImplementedException();
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
    }
}