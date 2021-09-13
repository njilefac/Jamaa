using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using Domain.Values;
using Libota.Application.Organisation;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Formatters.Abstractions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MemberRegistrationViewModel : ReactiveValidationObject
    {
        public MemberRegistrationViewModel(IOrganisationManagementFacade organisationManagementFacade,
            IScheduler? scheduler = null,
            IValidationTextFormatter<string>? formatter = null) : base(scheduler, formatter)
        {
            _organisationManagementFacade = organisationManagementFacade;
            GenderChoices = Enum.GetValues<Gender>().ToList();
            SelectedGender = Gender.Unknown;
        }

        public List<Gender> GenderChoices { get; }
        [Reactive] public string? FirstName { get; set; }
        [Reactive] public string? MiddleName { get; set; }
        [Reactive] public string? LastName { get; set; }
        [Reactive] public Gender SelectedGender { get; set; }
        [Reactive] public DateTimeOffset BirthDate { get; set; }
        [Reactive] public DateTimeOffset ValidFrom { get; set; }

        private readonly IOrganisationManagementFacade _organisationManagementFacade;
    }
}