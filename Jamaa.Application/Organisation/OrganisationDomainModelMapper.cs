using Domain.Members;
using Domain.Organisation;
using Jamaa.Data.Models.Members;
using Jamaa.Data.Models.Organisation;

namespace Jamaa.Application.Organisation;

internal static class OrganisationDomainModelMapper
{
    internal static OrganisationData ToDataModel(this OrganisationSummary organisation)
    {
        return new OrganisationData
        {
            Id = organisation.Id,
            Name = organisation.Name,
            Description = organisation.Description,
            Members = []
        };
    }

    internal static MemberData ToDataModel(this MemberProfile member)
    {
        var organisation = new OrganisationData
        {
            Id = member.OrganisationId.Value,
            Name = string.Empty,
            Description = null,
            Members = []
        };

        var mappedMember = new MemberData
        {
            Id = member.Id,
            FirstName = member.FirstName ?? string.Empty,
            MiddleName = member.MiddleName,
            LastName = member.LastName ?? string.Empty,
            Gender = member.Gender,
            Organisation = organisation,
            OrganisationId = member.OrganisationId.Value,
            Registration = null!,
            PictureData = member.PictureData
        };

        mappedMember.Registration = new RegistrationData
        {
            Id = member.RegistrationId,
            Member = mappedMember,
            MemberId = member.Id,
            StartDate = member.RegistrationBegin,
            EndDate = member.RegistrationEnd,
            MembershipType = member.MembershipType,
            Status = member.RegistrationStatus,
            Organisation = organisation
        };

        return mappedMember;
    }
}