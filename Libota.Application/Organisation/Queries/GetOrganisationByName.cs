using System;
using EventFlow.Queries;
using Libota.Application.Organisation.Queries.Models;

namespace Libota.Application.Organisation.Queries
{
    public class GetOrganisationByName : IQuery<OrganisationReadModel>
    {
        public string Name { get; }

        public GetOrganisationByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            Name = name;
        }
    }
}