using System;
using System.Collections.Generic;
using Domain.Finances;
using Domain.Members;
using Domain.Organisation.Values;

namespace Domain.Organisation.Entities;

public class Organisation
{
    /// <summary>
    /// A uid that uniquely identifies the organization
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// the name of the organization
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// a short description of the organization. 
    /// </summary>
    public string? Description { get; }

    public ISet<Member> Members { get; }

    /// <summary>
    ///  the set of dues that are applicable for the organization.
    /// </summary>
    public ISet<IDue> Dues { get; }

    /// <summary>
    /// Creates a new Organization or Association.
    /// </summary>
    /// <param name="name">the name of the organization</param>
    /// <param name="description">a short description of the purpose of the organization</param>
    public Organisation(string name, string? description)
    {
        Name = name;
        Description = description;
        Members = new HashSet<Member>();
        Dues = new HashSet<IDue>();
    }

    public Registration Register(Member member, MembershipType membershipType, DateTime registrationDate)
    {
        Members.Add(member);

        return new Registration(member, membershipType, registrationDate, null);
    }

    /// <summary>
    /// Adds a new <see cref="IDue"/> to the organization 
    /// </summary>
    /// <param name="due">the new due to be introduced</param>
    /// <returns>a reference to the new <see cref="IDue"/> that was added</returns>
    public IDue AddDue(IDue due)
    {
        throw new NotImplementedException();
    }
}