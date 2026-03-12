namespace WorkflowTemplate.Domain.Enumerations;

public enum TemplateStatus
{
    Draft, // The template is being created or modified and is not yet ready for use.
    Active, // The template is finalized and can be used to create workflow instances.
    Archived // The template is no longer active and should not be used for new workflow instances, but existing instances may still reference it.
}