using Domain.Organisation.Values;

namespace Libota.Application.Security.Authorization;

public record UserPermissions(
    OrganisationId OrganisationId,
    bool AllowUserManagement,
    bool AllowFinanceManagement,
    bool AllowSettingsManagement);