using Libota.Application.Users;

namespace Libota.Desktop.Security.Events;

public record UserAuthenticated(UserSession Session);