using Core.Shared.Errors;

namespace WorkflowTemplate.Shared.Errors;

public static class WorkflowTemplateErrors
{
    public static Error StepTitleAlreadyExists(string title) =>
        new ConflictError(new("WorkflowTemplate.StepTitleAlreadyExists",
            $"Step title '{title}' already exists."));

    public static Error TaskTitleAlreadyExists(string title) =>
        new ConflictError(new("WorkflowTemplate.TaskTitleAlreadyExists",
            $"Task title '{title}' already exists."));

    public static Error StepFieldLabelAlreadyExists(string label) =>
        new ConflictError(new("WorkflowTemplate.StepFieldLabelAlreadyExists",
            $"Step field label '{label}' already exists."));

    public static Error TaskFieldLabelAlreadyExists(string label) =>
        new ConflictError(new("WorkflowTemplate.TaskFieldLabelAlreadyExists",
            $"Task field label '{label}' already exists."));

    public static Error InvalidStatusTransition(string from, string to) =>
        new("WorkflowTemplate.InvalidStatusTransition",
            $"Cannot transition from '{from}' to '{to}'.");

    public static readonly Error ActivationRequiresStepWithTask =
        new("WorkflowTemplate.ActivationRequiresStepWithTask",
            "Template must have at least one step with at least one task to be activated.");

    public static Error StepNotFound(Guid stepId) =>
        new NotFoundError(new("WorkflowTemplate.StepNotFound",
            $"Step '{stepId}' was not found."));

    public static Error TaskNotFound(Guid taskId) =>
        new NotFoundError(new("WorkflowTemplate.TaskNotFound",
            $"Task '{taskId}' was not found."));

    public static Error StepFieldNotFound(Guid fieldId) =>
        new NotFoundError(new("WorkflowTemplate.StepFieldNotFound",
            $"Step field '{fieldId}' was not found."));

    public static Error TaskFieldNotFound(Guid fieldId) =>
        new NotFoundError(new("WorkflowTemplate.TaskFieldNotFound",
            $"Task field '{fieldId}' was not found."));
}