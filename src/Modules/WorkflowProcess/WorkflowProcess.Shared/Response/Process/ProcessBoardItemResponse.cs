namespace WorkflowProcess.Shared.Response.Process;

public sealed record ProcessBoardItemResponse(
    Guid Id,
    string ProcessType,
    string TargetClient,
    string Status,
    string? CurrentStepTitle,
    int TotalSteps,
    int CompletedSteps,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);