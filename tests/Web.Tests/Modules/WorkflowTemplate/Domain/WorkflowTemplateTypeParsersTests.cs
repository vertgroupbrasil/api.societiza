using WorkflowTemplate.Domain.Enumerations;

namespace Web.Tests.Modules.WorkflowTemplate.Domain;

public class WorkflowTemplateTypeParsersTests
{
    [Theory]
    [InlineData("Text", FieldType.Text)]
    [InlineData("text", FieldType.Text)]
    [InlineData("selection", FieldType.Select)]
    [InlineData("Select", FieldType.Select)]
    public void ParseFieldType_AcceptsCanonicalAndLegacyAliases(string value, FieldType expected)
    {
        var result = WorkflowTemplateTypeParsers.ParseFieldType(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Manual", TaskType.Manual)]
    [InlineData("Checklist", TaskType.Manual)]
    [InlineData("Document", TaskType.Manual)]
    [InlineData("Automatic", TaskType.Automatic)]
    [InlineData("Approval", TaskType.Approval)]
    public void ParseTaskType_AcceptsCanonicalAndLegacyAliases(string value, TaskType expected)
    {
        var result = WorkflowTemplateTypeParsers.ParseTaskType(value);

        Assert.Equal(expected, result);
    }
}
