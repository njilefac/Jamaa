using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Users;
using Jamaa.Data.Configuration;
using Jamaa.Data.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Jamaa.Data.Repositories.Users;

public class UserRepository(JamaaDbContext dbContext) : IUserRepository
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

    public async Task<User> Update(User user)
    {
        var existingUserData = await dbContext.Users.FindAsync(user.Account.Id);
        if (existingUserData == null) throw new KeyNotFoundException($"User with ID {user.Account.Id} not found.");

        // Update properties of the tracked entity
        existingUserData.DashboardLayout = user.DashboardLayout;
        existingUserData.UserName = user.Account.Credentials.UserName;
        existingUserData.Password = user.Account.Credentials.Password;
        existingUserData.Email = user.Account.Email;
        existingUserData.FirstName = user.FirstName;
        existingUserData.MiddleName = user.MiddleName;
        existingUserData.LastName = user.LastName;
        existingUserData.IsActive = user.Account.IsActive ?? false;
        existingUserData.IsSuperUser = user.Account.IsSuperUser ?? false;

        await dbContext.SaveChangesAsync();
        return UserData.Map(existingUserData);
    }

    public Task<bool> Delete(User user)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> GetById(Guid id)
    {
        var userData = await dbContext.Users.FindAsync(id);
        return userData != null ? UserData.Map(userData) : null;
    }

    public async Task<User?> SingleOrDefault(Predicate<User> matchesCondition)
    {
        var responseList = await dbContext.Users.ToListAsync();
        var allUsers = responseList.Select(UserData.Map).ToList();
        return allUsers.SingleOrDefault(x => matchesCondition(x));
    }
}