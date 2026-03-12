namespace WorkflowTemplate.Domain.Enumerations;

public enum TaskType
{
    Manual, // Requires user action to complete
    Automatic, // Completed automatically by the system based on predefined rules or conditions
    Approval // Requires approval from a designated user or group before proceeding to the next step
}