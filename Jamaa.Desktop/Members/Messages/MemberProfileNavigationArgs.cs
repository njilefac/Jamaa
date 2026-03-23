using Jamaa.Data.Models.Members;

namespace Jamaa.Desktop.Members.Messages;

public record MemberProfileNavigationArgs(MemberData Member, string? TargetTab = null);
