using Akka.Actor;
using Domain.Organisation.Values;
using Jamaa.Application.Accounting.Aggregates;
using Jamaa.Application.Accounting.Commands;
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
        ReceiveAsync<CreateAccount>(OnCreateAccount);
        ReceiveAsync<UpdateAccount>(OnUpdateAccount);
        ReceiveAsync<DeleteAccount>(OnDeleteAccount);
        ReceiveAsync<DeactivateAccount>(OnDeactivateAccount);
        ReceiveAsync<ReactivateAccount>(OnReactivateAccount);
        ReceiveAsync<CreateAccountingPeriod>(OnCreateAccountingPeriod);
        ReceiveAsync<UpdateAccountingPeriod>(OnUpdateAccountingPeriod);
        ReceiveAsync<DeleteAccountingPeriod>(OnDeleteAccountingPeriod);
        ReceiveAsync<UpdateAccountingSettings>(OnUpdateAccountingSettings);
        ReceiveAsync<SetAccountOpeningBalance>(OnSetAccountOpeningBalance);
    }

    private Task OnCreateFiscalYear(CreateFiscalYear command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateFiscalYear(UpdateFiscalYear command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeleteFiscalYear(DeleteFiscalYear command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnCreateAccount(CreateAccount command)
    {
        var accountAggregate = ResolveAccountAggregate(command.OrganisationId);
        accountAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateAccount(UpdateAccount command)
    {
        var accountAggregate = ResolveAccountAggregate(command.OrganisationId);
        accountAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeleteAccount(DeleteAccount command)
    {
        var accountAggregate = ResolveAccountAggregate(command.OrganisationId);
        accountAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeactivateAccount(DeactivateAccount command)
    {
        var accountAggregate = ResolveAccountAggregate(command.OrganisationId);
        accountAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnReactivateAccount(ReactivateAccount command)
    {
        var accountAggregate = ResolveAccountAggregate(command.OrganisationId);
        accountAggregate.Tell(command);
        return Task.CompletedTask;
    }

    // Integration: routes all account commands for one organisation through a single aggregate actor instance.
    private IActorRef ResolveAccountAggregate(OrganisationId organisationId)
    {
        var actorName = BuildAccountActorName(organisationId);
        var existing = Context.Child(actorName);
        if (!Equals(existing, ActorRefs.Nobody)) return existing;

        return Context.ActorOf(AccountAggregate.Props(organisationId, _queryProcessor), actorName);
    }

    // Operation: converts one organisation id into a valid deterministic Akka actor name.
    private static string BuildAccountActorName(OrganisationId organisationId)
    {
        var raw = organisationId.Value;
        var sanitized = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "accounts_default" : $"accounts_{sanitized}";
    }

    // Integration: routes all fiscal-calendar and period commands for one organisation through one aggregate instance.
    private IActorRef ResolveFiscalCalendarAggregate(OrganisationId organisationId)
    {
        var actorName = BuildFiscalCalendarActorName(organisationId);
        var existing = Context.Child(actorName);
        if (!Equals(existing, ActorRefs.Nobody)) return existing;

        return Context.ActorOf(FiscalCalendarAggregate.Props(organisationId), actorName);
    }

    // Operation: converts one organisation id into a valid deterministic Akka actor name for fiscal workflows.
    private static string BuildFiscalCalendarActorName(OrganisationId organisationId)
    {
        var raw = organisationId.Value;
        var sanitized = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "fiscal_default" : $"fiscal_{sanitized}";
    }

    private Task OnCreateAccountingPeriod(CreateAccountingPeriod command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateAccountingPeriod(UpdateAccountingPeriod command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnDeleteAccountingPeriod(DeleteAccountingPeriod command)
    {
        var fiscalCalendar = ResolveFiscalCalendarAggregate(command.OrganisationId);
        fiscalCalendar.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateAccountingSettings(UpdateAccountingSettings command)
    {
        var settingsAggregate = ResolveAccountingSettingsAggregate(command.OrganisationId);
        settingsAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnSetAccountOpeningBalance(SetAccountOpeningBalance command)
    {
        var balanceAggregate = ResolveAccountingPeriodBalanceAggregate(command.OrganisationId);
        balanceAggregate.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnUpdateMember(UpdateMember command)
    {
        var organisation = ResolveOrganisationAggregate(command.OrganisationId);
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnRegisterMember(RegisterMember command)
    {
        var organisation = ResolveOrganisationAggregate(command.OrganisationId);
        organisation.Tell(command);
        return Task.CompletedTask;
    }

    private Task OnCreateOrganisation(CreateOrganisation createCommand)
    {
        var newId = OrganisationId.With(Guid.NewGuid());
        var organisation = Context.ActorOf(OrganisationAggregate.Props(newId, _queryProcessor),
            BuildOrganisationActorName(newId));
        organisation.Tell(createCommand);
        return Task.CompletedTask;
    }

    // Integration: routes all organisation commands for one organisation through a single aggregate actor instance.
    private IActorRef ResolveOrganisationAggregate(OrganisationId organisationId)
    {
        var actorName = BuildOrganisationActorName(organisationId);
        var existing = Context.Child(actorName);
        if (!Equals(existing, ActorRefs.Nobody))
            return existing;

        return Context.ActorOf(OrganisationAggregate.Props(organisationId, _queryProcessor), actorName);
    }

    // Operation: converts one organisation id into a valid deterministic Akka actor name for organisation aggregates.
    private static string BuildOrganisationActorName(OrganisationId organisationId)
    {
        var raw = organisationId.Value;
        var sanitized = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "organisation_default" : $"organisation_{sanitized}";
    }

    // Integration: routes all accounting-settings commands for one organisation through a single aggregate actor instance.
    private IActorRef ResolveAccountingSettingsAggregate(OrganisationId organisationId)
    {
        var actorName = BuildAccountingSettingsActorName(organisationId);
        var existing = Context.Child(actorName);
        if (!Equals(existing, ActorRefs.Nobody))
            return existing;

        return Context.ActorOf(AccountingSettingsAggregate.Props(organisationId), actorName);
    }

    // Integration: routes all accounting-period-balance commands for one organisation through a single aggregate actor instance.
    private IActorRef ResolveAccountingPeriodBalanceAggregate(OrganisationId organisationId)
    {
        var actorName = BuildAccountingPeriodBalanceActorName(organisationId);
        var existing = Context.Child(actorName);
        if (!Equals(existing, ActorRefs.Nobody))
            return existing;

        return Context.ActorOf(AccountingPeriodBalanceAggregate.Props(organisationId), actorName);
    }

    // Operation: converts one organisation id into a valid deterministic Akka actor name for accounting-period-balance aggregates.
    private static string BuildAccountingPeriodBalanceActorName(OrganisationId organisationId)
    {
        var raw = organisationId.Value;
        var sanitized = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "balances_default" : $"balances_{sanitized}";
    }

    // Operation: converts one organisation id into a valid deterministic Akka actor name for accounting-settings aggregates.
    private static string BuildAccountingSettingsActorName(OrganisationId organisationId)
    {
        var raw = organisationId.Value;
        var sanitized = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return string.IsNullOrWhiteSpace(sanitized)
            ? "accounting_settings_default"
            : $"accounting_settings_{sanitized}";
    }
}