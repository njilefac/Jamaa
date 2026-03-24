using Jamaa.Application.Users;

namespace Jamaa.Desktop.Security.Events;

public record UserAuthenticated(UserSession Session);