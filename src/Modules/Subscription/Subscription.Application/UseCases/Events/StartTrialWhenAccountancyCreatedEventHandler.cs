using Core.Persistence.Context;
using Core.Persistence.UnitOfWork;
using Corporate.Domain.Events;
using MediatR;
using Subscription.Domain.Repositories;

namespace Subscription.Application.UseCases.Events;

public sealed class StartTrialWhenAccountancyCreatedEventHandler
    : INotificationHandler<DomainEvents.AccountancyCreated>
{
    private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;

    public StartTrialWhenAccountancyCreatedEventHandler(
        IUnitOfWork<ApplicationDbContext> unitOfWork,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository)
    {
        _unitOfWork = unitOfWork;
        _subscriptionRepository = subscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
    }

    public async Task Handle(DomainEvents.AccountancyCreated @event, CancellationToken cancellationToken)
    {
        var planTrial = await _subscriptionPlanRepository.GetByCodeAsync("trial", cancellationToken);

        if (planTrial is null)
            throw new InvalidOperationException("Plan 'trial' not found");

        if (await _subscriptionRepository.ExistsActiveOrTrialAsync(@event.TenantId, cancellationToken))
            return;

        var subscription = Domain.Entities.Subscription.StartTrial(
            tenantId: @event.TenantId,
            trialPlanId: planTrial.Id);

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            _subscriptionRepository.Add(subscription);
        }, cancellationToken);
    }
}