using System;
using System.Collections.Generic;
using Domain.Accounting;
using Domain.Accounting.Entities;
using Domain.Accounting.Values;
using Domain.Members;
using Domain.Organisation.Values;

namespace Domain.Organisation.Entities;

public class Organisation
{
    /// <summary>
    ///     Creates a new Organization or Association.
    /// </summary>
    /// <param name="name">the name of the organization</param>
    /// <param name="description">a short description of the purpose of the organization</param>
    public Organisation(string name, string? description)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        Members = new HashSet<Member>();
        Dues = new HashSet<IDue>();
        FiscalCalendar = new FiscalCalendar(FiscalCalendarId.New(), OrganisationId.With(Id));
        ChartOfAccounts = new ChartOfAccounts(ChartOfAccountsId.New(), OrganisationId.With(Id));
    }

    /// <summary>
    ///     A uid that uniquely identifies the organization
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     the name of the organization
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     a short description of the organization.
    /// </summary>
    public string? Description { get; }

    public ISet<Member> Members { get; }

    /// <summary>
    ///     the set of dues that are applicable for the organization.
    /// </summary>
    public ISet<IDue> Dues { get; }

    /// <summary>
    ///     The fiscal calendar owned by this organisation.
    /// </summary>
    public FiscalCalendar FiscalCalendar { get; }

    /// <summary>
    ///     The chart of accounts owned by this organisation.
    /// </summary>
    public ChartOfAccounts ChartOfAccounts { get; }

    public Registration Register(Member member, MembershipType membershipType, DateTime registrationDate)
    {
        Members.Add(member);

        return new Registration(member, membershipType, registrationDate, null);
    }
}