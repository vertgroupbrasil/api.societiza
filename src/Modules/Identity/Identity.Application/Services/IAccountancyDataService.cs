namespace Identity.Application.Services;

public interface IAccountancyDataService
{
    Task<bool> ExistsAsync(Guid accountancyId, CancellationToken cancellationToken);
}