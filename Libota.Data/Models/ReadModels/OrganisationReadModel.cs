using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using Libota.Application.Organisation;
using Libota.Application.Organisation.Events.Members;

namespace Libota.Data.Models.ReadModels
{
    public class OrganisationReadModel : IReadModel, IAmAsyncReadModelFor<OrganisationAggregate, OrganisationIdentity, MemberRegistrationUpdated>
    {
        public Task ApplyAsync(IReadModelContext context, IDomainEvent<OrganisationAggregate, OrganisationIdentity, MemberRegistrationUpdated> domainEvent, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}