using Domain.Values;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Members.Events
{
    [EventVersion("member-registered", 1)]
    public class MemberRegistered : AggregateEvent<OrganisationAggregate, OrganisationId>
    {
        public string FirstName { get; }
        public string? MiddleName { get; }
        public string LastName { get; }
        public Gender Gender { get; }
        public MembershipType MembershipType { get; set; }
    }
}