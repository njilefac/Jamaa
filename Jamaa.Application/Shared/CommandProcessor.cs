using Akka.Actor;
using Jamaa.Application.Finances.Aggregates;
using Jamaa.Application.Finances.Commands;
using Domain.Organisation.Values;
using Jamaa.Application.Organisation.Aggregates;
using Jamaa.Application.Organisation.Commands;

namespace Jamaa.Application.Shared;

public class CommandProcessor : ReceiveActor
{
    private readonly IQueryProcessor _queryProcessor;

    public CommandProcessor(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
        ReceiveAsync<CreateOrganisation>(OnCreateOrganisation);
        ReceiveAsync<RegisterMember>(OnRegisterMember);
        ReceiveAsync<UpdateMember>(OnUpdateMember);
        ReceiveAsync<CreateFiscalYear>(OnCreateFiscalYear);
        ReceiveAsync<UpdateFiscalYear>(OnUpdateFiscalYear);
        ReceiveAsync<DeleteFiscalYear>(OnDeleteFiscalYear);
        ReceiveAsync<CreateAccountingPeriod>(OnCreateAccountingPeriod);
        ReceiveAsync<UpdateAccountingPeriod>(OnUpdateAccountingPeriod);
        ReceiveAsync<DeleteAccountingPeriod>(OnDeleteAccountingPeriod);
        ReceiveAsync<UpdateAccountingSettings>(OnUpdateAccountingSettings);
    }

    private Task OnCreateFiscalYear(CreateFiscalYear command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateFiscalYear(UpdateFiscalYear command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeleteFiscalYear(DeleteFiscalYear command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnCreateAccountingPeriod(CreateAccountingPeriod command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateAccountingPeriod(UpdateAccountingPeriod command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeleteAccountingPeriod(DeleteAccountingPeriod command)
    {
        var fiscalCalendar = Context.ActorOf(FiscalCalendarAggregate.Props(command.OrganisationId));
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateAccountingSettings(UpdateAccountingSettings command)
    {
        var settingsAggregate = Context.ActorOf(AccountingSettingsAggregate.Props(command.OrganisationId));
        settingsAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateMember(UpdateMember command)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(command.OrganisationId, _queryProcessor));
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnRegisterMember(RegisterMember command)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(command.OrganisationId, _queryProcessor));
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnCreateOrganisation(CreateOrganisation createCommand)
    {
        var organisation = Context.ActorOf(OrganisationAggregate.Props(OrganisationId.With(Guid.NewGuid()), _queryProcessor));
        organisation.Tell(createCommand);
        return Task.CompletedTask;
    }
}