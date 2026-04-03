namespace WorkflowTemplate.Domain.Enumerations;

public static class WorkflowTemplateTypeParsers
{
    // Keep these lists in sync with the parser and the public API error messages.
    public static IReadOnlyList<string> SupportedFieldTypes { get; } =
    [
        nameof(FieldType.Text),
        nameof(FieldType.Boolean),
        nameof(FieldType.Integer),
        nameof(FieldType.Select),
        nameof(FieldType.Date),
    ];

    public static IReadOnlyList<string> SupportedTaskTypes { get; } =
    [
        nameof(TaskType.Manual),
        nameof(TaskType.Automatic),
        nameof(TaskType.Approval),
    ];

    public static bool TryParseFieldType(string? value, out FieldType fieldType)
    {
        // Normalization allows older payloads and localized aliases to map to the
        // canonical enum without breaking existing templates.
        switch (Normalize(value))
        {
            case "text":
            case "texto":
                fieldType = FieldType.Text;
                return true;
            case "boolean":
            case "bool":
                fieldType = FieldType.Boolean;
                return true;
            case "integer":
            case "int":
            case "number":
            case "numero":
                fieldType = FieldType.Integer;
                return true;
            case "select":
            case "selection":
            case "selecao":
            case "seleção":
                fieldType = FieldType.Select;
                return true;
            case "date":
            case "data":
                fieldType = FieldType.Date;
                return true;
            default:
                fieldType = default;
                return false;
        }
    }

    public static FieldType ParseFieldType(string? value)
        => TryParseFieldType(value, out var fieldType)
            ? fieldType
            : throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Unsupported field type. Expected one of: {string.Join(", ", SupportedFieldTypes)}");

    public static bool TryParseTaskType(string? value, out TaskType taskType)
    {
        // Tasks were historically named with several aliases; preserve them here
        // so legacy data and older clients still deserialize correctly.
        switch (Normalize(value))
        {
            case "manual":
            case "form":
            case "checklist":
            case "document":
            case "documento":
                taskType = TaskType.Manual;
                return true;
            case "automatic":
            case "automatico":
            case "automático":
            case "auto":
                taskType = TaskType.Automatic;
                return true;
            case "approval":
            case "aprovacao":
            case "aprovação":
                taskType = TaskType.Approval;
                return true;
            default:
                taskType = default;
                return false;
        }
    }

    public static TaskType ParseTaskType(string? value)
        => TryParseTaskType(value, out var taskType)
            ? taskType
            : throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Unsupported task type. Expected one of: {string.Join(", ", SupportedTaskTypes)}");

    private static string Normalize(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
