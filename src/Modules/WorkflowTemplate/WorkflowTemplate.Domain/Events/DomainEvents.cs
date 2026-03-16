using Core.Domain.Events;
using Core.Domain.Events.Interfaces;

namespace WorkflowTemplate.Domain.Events;

public static class DomainEvents
{
    #region Workflow Template Events
    public sealed record WorkflowTemplateCreated(
        Guid Id,
        string Name,
        string Description,
        string TemplateVersion,
        string Status,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record WorkflowTemplateUpdated(
        Guid Id,
        string Name,
        string Description,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record WorkflowTemplateActivated(
        Guid Id,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public sealed record WorkflowTemplateArchived(
        Guid Id,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Workflow Template Step Events
    public record WorkflowTemplateStepAdded(
        Guid TemplateId,
        Guid StepId,
        string Title,
        string Description,
        int Order,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepUpdated(
        Guid TemplateId,
        Guid StepId,
        string Title,
        string Description,
        int Order,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepRemoved(
        Guid TemplateId,
        Guid StepId,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepCascadeDeleted(
        Guid TemplateId,
        Guid StepId,
        List<Guid> FieldIds,
        List<Guid> TaskIds,
        List<Guid> TaskFieldIds,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Workflow Template Step Tasks Events
    public record WorkflowTemplateStepTaskAdded(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        string Title,
        string Description,
        string Type,
        string Configuration,
        int Order,
        bool IsOptional,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepTaskUpdated(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        string Title,
        string Description,
        string Type,
        string Configuration,
        int Order,
        bool IsOptional,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepTaskRemoved(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepTaskCascadeDeleted(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        List<Guid> TaskFieldIds,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Workflow Template Step Field Events
    public record WorkflowTemplateStepFieldAdded(
        Guid TemplateId,
        Guid StepId,
        Guid FieldId,
        string Label,
        string FieldType,
        string? Options,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepFieldUpdated(
        Guid TemplateId,
        Guid StepId,
        Guid FieldId,
        string Label,
        string FieldType,
        string? Options,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepFieldRemoved(
        Guid TemplateId,
        Guid StepId,
        Guid FieldId,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion

    #region Workflow Template Step Task Field Events
    public record WorkflowTemplateStepTaskFieldAdded(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        Guid FieldId,
        string Label,
        string FieldType,
        string? Options,
        DateTimeOffset CreatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepTaskFieldUpdated(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        Guid FieldId,
        string Label,
        string FieldType,
        string? Options,
        DateTimeOffset UpdatedAt,
        ulong Version
    ) : Message, IDomainEvent;

    public record WorkflowTemplateStepTaskFieldRemoved(
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        Guid FieldId,
        ulong Version
    ) : Message, IDomainEvent;
    #endregion
}