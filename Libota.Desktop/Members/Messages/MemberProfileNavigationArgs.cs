using Libota.Data.Models.Members;

namespace Libota.Desktop.Members.Messages;

public record MemberProfileNavigationArgs(MemberData Member, string? TargetTab = null);
