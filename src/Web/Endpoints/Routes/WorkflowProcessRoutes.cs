namespace Web.Endpoints.Routes;

internal static class WorkflowProcessRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/workflow-process";

    internal const string ProcessId      = "processId";
    internal const string StepInstanceId = "stepInstanceId";
    internal const string TaskInstanceId = "taskInstanceId";
    internal const string FieldInstanceId = "fieldInstanceId";

    internal const string CreateWorkflowProcess  = BaseUri;
    internal const string GetProcessBoard        = BaseUri;
    internal const string ListProcessesByStep    = $"{BaseUri}/by-step";
    internal const string GetProcessDetail       = $"{BaseUri}/{{{ProcessId}:guid}}";

    internal const string CompleteProcessTask = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/complete";
    internal const string SkipProcessTask     = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/skip";
    internal const string RevertProcessTask   = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/revert";

    internal const string FillProcessStepField     = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/fields/{{{FieldInstanceId}:guid}}";
    internal const string FillProcessTaskField     = $"{BaseUri}/{{{ProcessId}:guid}}/steps/{{{StepInstanceId}:guid}}/tasks/{{{TaskInstanceId}:guid}}/fields/{{{FieldInstanceId}:guid}}";
}