using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Jamaa.Application.Accounting.Events;
using Jamaa.Application.Shared;
using Domain.Accounting.Values;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Jamaa.Data.Configuration;
using Jamaa.Data.Notifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Shared;

public class JamaaEventTaggerTests
{
    [Fact]
    public void ToJournal_ShouldTagAccountOpeningBalanceSetWithOrganisationEvent()
    {
        // Arrange
        var tagger = new JamaaEventTagger();
        var evt = new AccountOpeningBalanceSet(
            OrganisationId.With("org-1"),
            AccountId.With("acc-1"),
            FiscalYearId.With("fy-1"),
            AccountingPeriodId.With("p-1"),
            100m
        );

        // Act
        var result = tagger.ToJournal(evt);

        // Assert
        result.ShouldBeOfType<Tagged>();
        var tagged = (Tagged)result;
        tagged.Payload.ShouldBe(evt);
        tagged.Tags.ShouldContain(JamaaEventTagger.OrganisationEvent);
        tagged.Tags.ShouldContain(JamaaEventTagger.FinanceChanged);
    }
}
