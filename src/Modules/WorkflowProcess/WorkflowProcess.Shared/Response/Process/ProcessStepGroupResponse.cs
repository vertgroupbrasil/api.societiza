namespace WorkflowProcess.Shared.Response.Process;

public sealed record ProcessStepGroupResponse(
    string StepTitle,
    int StepOrder,
    int ProcessCount,
    List<ProcessByStepItemResponse> Processes
);
