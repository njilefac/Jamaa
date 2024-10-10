using FluentValidation;
using Libota.Desktop.Assets.Resources;
using Libota.Desktop.ViewModels.Security;

namespace Libota.Desktop.Validators;

public class LoginScreenViewModelValidator : AbstractValidator<LoginScreenViewModel>
{
    public LoginScreenViewModelValidator()
    {
        RuleFor(vm => vm.UserName)
            .MinimumLength(3)
            .WithMessage(Messages.login_error_username);
        RuleFor(vm => vm.Password)
            .MinimumLength(6)
            .WithMessage(Messages.login_error_password);
    }
}