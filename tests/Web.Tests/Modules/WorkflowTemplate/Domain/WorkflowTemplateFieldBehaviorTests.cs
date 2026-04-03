using WorkflowTemplate.Domain.Aggregates;
using WorkflowTemplate.Domain.Enumerations;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Domain;

public class WorkflowTemplateFieldBehaviorTests
{
    [Fact]
    public void AddStepField_AllowsDuplicateLabels_AndPreservesOrder()
    {
        var template = WorkflowTemplateAggregate.Create("Template", "Descricao");
        var stepId = template.AddStep("Etapa 1", "Descricao", 1);

        template.AddStepField(stepId, "Documento", FieldType.Text, null, 1);
        template.AddStepField(stepId, "Documento", FieldType.Text, null, 2);

        var step = template.Steps.Single();

        Assert.Equal(2, step.Fields.Count);
        Assert.Equal([1, 2], step.Fields.Select(field => field.Order));
    }

    [Fact]
    public void AddTaskField_AllowsDuplicateLabels_AndPreservesOrder()
    {
        var template = WorkflowTemplateAggregate.Create("Template", "Descricao");
        var stepId = template.AddStep("Etapa 1", "Descricao", 1);
        var taskId = template.AddTask(stepId, "Tarefa 1", "Descricao", TaskType.Manual, string.Empty, 1, false);

        template.AddTaskField(stepId, taskId, "Documento", FieldType.Select, "[\"A\"]", 1);
        template.AddTaskField(stepId, taskId, "Documento", FieldType.Select, "[\"B\"]", 2);

        var task = template.Steps.Single().Tasks.Single();

        Assert.Equal(2, task.Fields.Count);
        Assert.Equal([1, 2], task.Fields.Select(field => field.Order));
    }
}
