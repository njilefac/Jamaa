using Domain.Entities.Shared;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using Libota.Application.Organisation.Aggregates;

namespace Libota.Application.Organisation.Commands
{
    public class UpdateOrganisationContactDetails : Command<OrganisationAggregate, OrganisationId, IExecutionResult>
    {
        public Address Address { get; }

        public UpdateOrganisationContactDetails(OrganisationId aggregateId, Address address) : base(aggregateId)
        {
            Address = address;
        }
    }
}