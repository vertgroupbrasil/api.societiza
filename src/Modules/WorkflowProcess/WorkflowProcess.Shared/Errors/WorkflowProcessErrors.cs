using Core.Shared.Errors;

namespace WorkflowProcess.Shared.Errors;

public static class WorkflowProcessErrors
{
    public static readonly Error TemplateNotActiveOrNotFound =
        new NotFoundError(new("WorkflowProcess.TemplateNotActiveOrNotFound",
            "Template not found or not in Active status."));
}
