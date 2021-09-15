using System;
using System.Reactive.Linq;
using EntityFrameworkCore.Rx;
using Libota.Application.Members.Queries.Models;
using Libota.Application.Shared.Providers;
using Libota.Data.Configuration;

namespace Libota.Data.Providers
{
    public class MembersInsertedProvider : IProvideObservableData<Member>
    {
        public IObservable<Member> Stream => DbObservable<LibotaDbContext>
            .FromInserted<Member>()
            .Select(x => x.Service);
    }
}