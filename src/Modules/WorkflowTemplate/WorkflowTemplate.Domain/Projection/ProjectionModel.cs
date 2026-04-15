using Core.Domain.Primitives.Interfaces;

namespace WorkflowTemplate.Domain.Projection;

public static class ProjectionModel
{
    public record WorkflowTemplate(
        Guid Id,
        string Name,
        string Description,
        string TemplateVersion,
        string Status,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel { };

    public record WorkflowTemplateStep(
        Guid Id,
        Guid TemplateId,
        string Title,
        string Description,
        int Order,
        List<string> AppliesTo,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };

    public record WorkflowTemplateStepTask(
        Guid Id,
        Guid TemplateId,
        Guid StepId,
        string Title,
        string Description,
        string Type,
        string Configuration,
        int Order,
        bool IsOptional,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };

    public record TemplateStepField(
        Guid Id,
        Guid TemplateId,
        Guid StepId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };

    public record TemplateStepTaskField(
        Guid Id,
        Guid TemplateId,
        Guid StepId,
        Guid TaskId,
        string Label,
        string FieldType,
        string? Options,
        int Order,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };
}