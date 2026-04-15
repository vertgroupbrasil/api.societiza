using Core.Shared.Errors;

namespace WorkflowProcess.Shared.Errors;

public static class WorkflowProcessErrors
{
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
}