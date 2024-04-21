using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Domain.Members.Queries;
using Libota.Data.Configuration;
using Libota.Data.Models.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Libota.Data.Queries.Members
{
    public class MembersQueryHandler : ReceiveActor
    {
        private readonly LibotaDbContext _dbContext;

        public MembersQueryHandler(IServiceProvider sp)
        {
            _dbContext = sp.GetRequiredService<LibotaDbContext>();
            
            ReceiveAsync<GetMembersByOrganisation>(OnGetMembersByOrganisation);
        }

        private async Task OnGetMembersByOrganisation(GetMembersByOrganisation query)
        {
            var organisations = await _dbContext.Organisations
                .Include(x => x.Members)
                .ThenInclude(member => member.Registration)
                .ToListAsync();

            var matchingOrganisation = organisations.FirstOrDefault(x =>
                x.Id != null && x.Id.Equals(query.OrganisationId.Value, StringComparison.InvariantCultureIgnoreCase));
            
            Sender.Tell(matchingOrganisation != null ? matchingOrganisation.Members : new List<Member>());
        }
        
        public static Props Props(IServiceProvider sp)
        {
            return new Props(typeof(MembersQueryHandler), [sp]);
        }
    }
}