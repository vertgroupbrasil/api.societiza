using Core.Shared.Errors;

namespace WorkflowProcess.Shared.Errors;

public static class WorkflowProcessErrors
{
    public static readonly Error Forbidden =
        new ForbiddenError(new("WorkflowProcess.Forbidden",
            "You do not have permission to access this workflow process."));

    public static readonly Error TemplateNotActiveOrNotFound =
        new NotFoundError(new("WorkflowProcess.TemplateNotActiveOrNotFound",
            "Template not found or not in Active status."));

    public static readonly Error ProcessNotFound =
        new NotFoundError(new("WorkflowProcess.NotFound",
            "Workflow process not found."));

    public static readonly Error ProcessAlreadyCompleted =
        new ConflictError(new("WorkflowProcess.AlreadyCompleted",
            "Workflow process is already completed and cannot be modified."));

    public static readonly Error StepNotFound =
        new NotFoundError(new("WorkflowProcess.StepNotFound",
            "Process step instance not found."));

    public static readonly Error TaskNotFound =
        new NotFoundError(new("WorkflowProcess.TaskNotFound",
            "Process task instance not found."));

    public static readonly Error TaskAlreadyResolved =
        new ConflictError(new("WorkflowProcess.TaskAlreadyResolved",
            "Task is already completed or skipped."));

    public static readonly Error StepFieldNotFound =
        new NotFoundError(new("WorkflowProcess.StepFieldNotFound",
            "Process step field instance not found."));

    public static readonly Error TaskFieldNotFound =
        new NotFoundError(new("WorkflowProcess.TaskFieldNotFound",
            "Process task field instance not found."));

    public static readonly Error StepNotUnlocked =
        new ConflictError(new("WorkflowProcess.StepNotUnlocked",
            "This step is locked. Complete all previous steps before proceeding."));

    public static readonly Error TaskNotRevokable =
        new ConflictError(new("WorkflowProcess.TaskNotRevokable",
            "Task is already pending and cannot be reverted."));

    public static Error PreviousRequiredTaskNotResolved(string blockingTaskTitle) =>
        new ConflictError(new("WorkflowProcess.PreviousRequiredTaskNotResolved",
            $"Task '{blockingTaskTitle}' must be completed before proceeding to the next task."));

    public static Error DownstreamStepHasProgress(string stepTitle) =>
        new ConflictError(new("WorkflowProcess.DownstreamStepHasProgress",
            $"Step '{stepTitle}' has progress. Revert it before reverting this step."));
}