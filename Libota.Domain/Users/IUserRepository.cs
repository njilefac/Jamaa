using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Users;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAll();
    Task<User> Add(User newUser);
    Task<User> Update(User user);
    Task<bool> Delete(User user);
    Task<User> GetById(Guid id);
    Task<User?> SingleOrDefault(Predicate<User> matchesCondition);
}