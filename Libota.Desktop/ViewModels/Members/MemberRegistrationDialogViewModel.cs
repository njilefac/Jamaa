using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Domain.Organisation.Requests;
using Domain.Organisation.Values;
using Domain.Shared.Values;
using Libota.Application.Users.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Formatters.Abstractions;
using ReactiveUI.Validation.Helpers;

namespace Libota.Desktop.ViewModels.Members
{
    public class MemberRegistrationDialogViewModel : ReactiveValidationObject
    {
        private readonly IUserSessionService _userSessionService;
        public MemberRegistrationDialogViewModel(IUserSessionService userSessionService, IScheduler? scheduler = null,
            IValidationTextFormatter<string>? formatter = null) : base(scheduler, formatter)
        {
            _userSessionService = userSessionService;
            GenderChoices = Enum.GetValues<Gender>().ToList();
            SelectedGender = Gender.Unknown;
            MembershipType = MembershipType.Regular;
            RegistrationBegin = DateTime.Today;

            this.ValidationRule(x => x.FirstName, v => !string.IsNullOrWhiteSpace(v) && v.Length > 2,
                "First name must be at least three characters");

            this.ValidationRule(x => x.LastName, v => !string.IsNullOrWhiteSpace(v) && v.Length > 2,
                "Last name must be at least three characters");

            this.ValidationRule(x => x.SelectedGender, v => v != Gender.Unknown,
                "Please select a gender");
            
            RegisterMember =
                ReactiveCommand.CreateFromTask<Unit, MemberRegistrationRequest>(GetRegistrationData, this.IsValid());
        }

        private async Task<MemberRegistrationRequest> GetRegistrationData(Unit arg)
        {
            var organisationId = _userSessionService!.CurrentUserSession!.Organisation?.Id;
            
            var request = new MemberRegistrationRequest
            {
                FirstName = FirstName,
                MiddleName = MiddleName,
                LastName = LastName,
                Gender = SelectedGender,
                RegistrationBegin = RegistrationBegin,
                MembershipType = MembershipType,
                OrganisationId = OrganisationId.With(organisationId),
            };
            
            return await Task.FromResult(request);
        }

        public List<Gender> GenderChoices { get; }
        [Reactive] public string? FirstName { get; set; }
        [Reactive] public string? MiddleName { get; set; }
        [Reactive] public string? LastName { get; set; }
        [Reactive] public Gender SelectedGender { get; set; }
        
        [Reactive] public DateTime DateOfBirth { get; set; }
        [Reactive] public DateTime RegistrationBegin { get; set; }
        [Reactive] public MembershipType MembershipType { get; set; }

        public ReactiveCommand<Unit, MemberRegistrationRequest> RegisterMember { get; set; }
    }
}