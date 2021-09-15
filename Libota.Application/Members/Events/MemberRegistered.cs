using System;
using Domain.Values;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Members.Events
{
    [EventVersion("member-registered", 1)]
    public class MemberRegistered : AggregateEvent<OrganisationAggregate, OrganisationId>
    {
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        
        public DateTime BirthDate { get; set; }
        public DateTime RegistrationBegin { get; set; }
        public MembershipType MembershipType { get; set; }
    }
}