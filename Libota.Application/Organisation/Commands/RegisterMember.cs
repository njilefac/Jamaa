using Domain.Organisation.Requests;

namespace Libota.Application.Organisation.Commands
{
    public class RegisterMember
    {
        public MemberRegistrationRequest RegistrationRequest { get; }

        public RegisterMember(MemberRegistrationRequest registrationRequest)
        {
            RegistrationRequest = registrationRequest;
        }
    }
}