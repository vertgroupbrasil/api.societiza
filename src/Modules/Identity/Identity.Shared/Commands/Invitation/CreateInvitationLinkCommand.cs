using Core.Application.Messaging.Commands;
using Identity.Shared.Response.Invitation;

namespace Identity.Shared.Commands.Invitation;

public sealed record CreateInvitationLinkCommand(
    string TargetRole,
    Guid? AccountancyId,
    int? MaxUses
) : ICommand<CreateInvitationLinkResponse>;