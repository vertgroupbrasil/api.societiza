namespace Core.Application.Services.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Role { get; }
    Guid? AccountancyId { get; }
    Guid? ResolveAccountancyId(Guid? routeAccountancyId);
}
