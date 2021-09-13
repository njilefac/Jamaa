using System.Threading.Tasks;
using Libota.Application.Organisation.Requests;

namespace Libota.Application.Organisation
{
    public interface IOrganisationManagementFacade
    {
        Task RegisterMember(MemberRegistrationRequest request);
    }
}