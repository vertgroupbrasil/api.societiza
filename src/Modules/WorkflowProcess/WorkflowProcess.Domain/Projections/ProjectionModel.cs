using Core.Domain.Primitives.Interfaces;

namespace WorkflowProcess.Domain.Projections;

public static class ProjectionModel
{
    public record WorkflowProcess(
        Guid Id,
        Guid AccountancyId,
        Guid TemplateId,
        string TemplateVersion,
        string ProcessType,
        string TargetClient,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel { };

    public record ProcessStepInstance(
        Guid Id,
        Guid ProcessId,
        Guid TemplateStepId,
        string Title,
        int Order,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };

    public record ProcessStepTaskInstance(
        Guid Id,
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TemplateTaskId,
        string Title,
        int Order,
        bool IsOptional,
        string Status,
        DateTimeOffset? CompletedAt,
        DateTimeOffset? SkippedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };

    public record ProcessStepFieldInstance(
        Guid Id,
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TemplateFieldId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        string? Value,
        DateTimeOffset? FilledAt,
        DateTimeOffset CreatedAt
    ) : IProjectionModel { };

    public record ProcessStepTaskFieldInstance(
        Guid Id,
        Guid ProcessId,
        Guid StepInstanceId,
        Guid TaskInstanceId,
        Guid TemplateFieldId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        string? Value,
        DateTimeOffset? FilledAt,
        DateTimeOffset CreatedAt
    ) : IProjectionModel { };
}