using System;
using Domain.Shared;

namespace Domain.Users;

public class User(
    string userName,
    string password,
    string email,
    string? firstName,
    string? middleName,
    string? lastName,
    bool isSuperUser = false,
    bool isActive = true,
    string? dashboardLayout = null,
    Guid? id = null)
    : Person(firstName, middleName, lastName)
{
    public UserAccount Account => new(userName, password, email, isSuperUser, isActive, id);
    public string? DashboardLayout { get; set; } = dashboardLayout;
    public Guid? Id => id;
}