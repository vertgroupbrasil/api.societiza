namespace Web.Endpoints.Routes;

internal static class WorkflowTemplateRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/workflow-template";

    internal const string WorkflowTemplateId = "workflowTemplateId";
    internal const string WorkflowStepId = "workflowStepId";
    internal const string WorkflowTaskId = "workflowTaskId";
    internal const string WorkflowFieldId = "workflowFieldId";

    // Workflow Template Routes
    internal const string CreateWorkflowTemplate = $"{BaseUri}";
    internal const string UpdateWorkflowTemplate = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}";
    internal const string ActivateWorkflowTemplate = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/activate";
    internal const string ArchiveWorkflowTemplate = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/archive";
    internal const string DeleteWorkflowTemplate = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}";

    internal const string GetAllWorkflowTemplates = BaseUri;
    internal const string GetWorkflowTemplateById = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}";

    // Workflow Template Step Routes
    internal const string AddWorkflowTemplateStep = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step";
    internal const string UpdateWorkflowTemplateStep = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}";
    internal const string RemoveWorkflowTemplateStep = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}";

    // Workflow Template Step Task Routes
    internal const string AddWorkflowTemplateStepTask = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task";
    internal const string UpdateWorkflowTemplateStepTask = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task/{{{WorkflowTaskId}:guid}}";
    internal const string RemoveWorkflowTemplateStepTask = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task/{{{WorkflowTaskId}:guid}}";

    // Workflow Template Step Field Routes
    internal const string AddTemplateStepField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/fields";
    internal const string UpdateTemplateStepField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/fields/{{{WorkflowFieldId}:guid}}";
    internal const string RemoveTemplateStepField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/fields/{{{WorkflowFieldId}:guid}}";

    // Workflow Template Step Task Field Routes
    internal const string AddTemplateStepTaskField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task/{{{WorkflowTaskId}:guid}}/fields";
    internal const string UpdateTemplateStepTaskField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task/{{{WorkflowTaskId}:guid}}/fields/{{{WorkflowFieldId}:guid}}";
    internal const string RemoveTemplateStepTaskField = $"{BaseUri}/{{{WorkflowTemplateId}:guid}}/step/{{{WorkflowStepId}:guid}}/task/{{{WorkflowTaskId}:guid}}/fields/{{{WorkflowFieldId}:guid}}";
}