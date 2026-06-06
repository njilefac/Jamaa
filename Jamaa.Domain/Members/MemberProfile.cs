using System;
using Domain.Organisation.Values;
using Domain.Shared.Values;

namespace Domain.Members;

public record MemberProfile(
    string Id,
    string RegistrationId,
    OrganisationId OrganisationId,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    Gender Gender,
    MembershipType MembershipType,
    RegistrationStatus RegistrationStatus,
    DateTime RegistrationBegin,
    DateTime? RegistrationEnd,
    byte[]? PictureData);