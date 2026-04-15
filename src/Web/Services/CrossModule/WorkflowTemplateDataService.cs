using MediatR;
using WorkflowProcess.Application.Services;
using WorkflowProcess.Shared.Snapshots;
using WorkflowTemplate.Shared.Queries.Template;

namespace Web.Services.CrossModule;

public sealed class WorkflowTemplateDataService(ISender sender) : IWorkflowTemplateDataService
{
    public async Task<(string Version, List<TemplateStepSnapshot> Steps)?> GetActiveTemplateSnapshotAsync(
        Guid templateId, string processType, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkflowTemplateDetailByIdQuery(templateId), cancellationToken);

        if (!result.IsSuccess || result.Value.Status != "Active")
            return null;

        var snapshots = result.Value.Steps
            .Where(step => step.AppliesTo.Count == 0 || step.AppliesTo.Contains(processType))
            .Select(step => new TemplateStepSnapshot(
                StepId: step.Id,
                Title: step.Title,
                Description: step.Description,
                Order: step.Order,
                Tasks: step.Tasks.Select(task => new TemplateStepTaskSnapshot(
                    TaskId: task.Id,
                    Title: task.Title,
                    Description: task.Description,
                    Type: task.Type,
                    Configuration: task.Configuration,
                    Order: task.Order,
                    IsOptional: task.IsOptional,
                    Fields: task.Fields.Select(f => new TemplateStepTaskFieldSnapshot(
                        FieldId: f.Id,
                        Label: f.Label,
                        FieldType: f.FieldType,
                        Options: f.Options,
                        Order: f.Order
                    )).ToList()
                )).ToList(),
                Fields: step.Fields.Select(field => new TemplateStepFieldSnapshot(
                    FieldId: field.Id,
                    Label: field.Label,
                    FieldType: field.FieldType,
                    Options: field.Options,
                    Order: field.Order
                )).ToList()
            ))
            .ToList();

        return (result.Value.Version, snapshots);
    }
}