using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace WorkflowProcess.Domain.Events;

public static class DomainEvents
{
    #region Workflow Process Events
    public sealed record WorkflowProcessCreated(
        Guid Id,
        Guid AccountancyId,
        Guid TemplateId,
        string TemplateVersion,
        string ProcessType,
        string TargetClient,
        string Status,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record WorkflowProcessCompleted(
        Guid ProcessId,
        DateTimeOffset CompletedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record WorkflowProcessReverted(
        Guid ProcessId,
        DateTimeOffset RevertedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Process Step Instance Events
    public sealed record ProcessStepInstanceMaterialized(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TemplateStepId,
        string Title,
        int Order,
        string Status,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepInstanceStarted(
        Guid ProcessId,
        Guid StepInstanceId,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepInstanceCompleted(
        Guid ProcessId,
        Guid StepInstanceId,
        DateTimeOffset CompletedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepInstanceReverted(
        Guid ProcessId,
        Guid StepInstanceId,
        string NewStatus,
        DateTimeOffset RevertedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Process Step Task Instance Events
    public sealed record ProcessStepTaskInstanceMaterialized(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        Guid TemplateTaskId,
        string Title,
        int Order,
        bool IsOptional,
        string Status,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepTaskInstanceCompleted(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        DateTimeOffset CompletedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepTaskInstanceSkipped(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        DateTimeOffset SkippedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepTaskInstanceReverted(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        DateTimeOffset RevertedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Process Step Field Instance Events
    public sealed record ProcessStepFieldInstanceMaterialized(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid FieldInstanceId,
        Guid TemplateFieldId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepFieldInstanceFilled(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid FieldInstanceId,
        string Value,
        DateTimeOffset FilledAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Process Step Task Field Instance Events
    public sealed record ProcessStepTaskFieldInstanceMaterialized(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        Guid FieldInstanceId,
        Guid TemplateFieldId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record ProcessStepTaskFieldInstanceFilled(
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        Guid FieldInstanceId,
        string Value,
        DateTimeOffset FilledAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion
}