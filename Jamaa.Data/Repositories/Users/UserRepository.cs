using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Users;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Repositories.Users;

public class UserRepository(LibotaDbContext dbContext) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAll()
    {
        var userDataSet = await dbContext.Users.AsNoTracking().ToListAsync();
        return userDataSet.Select(UserData.Map);
    }

    public async Task<User> Add(User newUser)
    {
        var userData = UserData.Map(newUser);
        var result = await dbContext.Users.AddAsync(userData);
        await dbContext.SaveChangesAsync();
        return UserData.Map(result.Entity);
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

    public async Task<User?> SingleOrDefault(Predicate<User> matchesCondition)
    {
        var responseList = await dbContext.Users.ToListAsync();
        var allUsers =  responseList.Select(UserData.Map).ToList();
        return allUsers.SingleOrDefault(x => matchesCondition(x));
    }
}