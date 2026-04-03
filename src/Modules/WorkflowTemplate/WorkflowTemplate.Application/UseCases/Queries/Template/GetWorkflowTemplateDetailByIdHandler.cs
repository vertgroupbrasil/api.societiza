using Core.Application.Messaging.Queries;
using Core.Shared.Errors;
using Core.Shared.Results;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;
using WorkflowTemplate.Shared.Response.TemplateStep;
using WorkflowTemplate.Shared.Response.TemplateStepTask;

namespace WorkflowTemplate.Application.UseCases.Queries.Template;

public class GetWorkflowTemplateDetailByIdHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate> templateProjection,
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep> stepProjection,
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask> taskProjection,

    IWorkflowTemplateProjection<ProjectionModel.TemplateStepField> stepFielProjection,
    IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField> taskFieldProjection) : IQueryHandler<GetWorkflowTemplateDetailByIdQuery, WorkflowTemplateDetailResponse>
{
    public async Task<Result<WorkflowTemplateDetailResponse>> Handle(GetWorkflowTemplateDetailByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await templateProjection.GetByIdAsync(request.Id, cancellationToken);

        if (template == null)
            return Result.Failure<WorkflowTemplateDetailResponse>(new NotFoundError(new(
                "WorkflowTemplateNotFound", $"Workflow template {request.Id} not found")));

        var stepsQuery = stepProjection.ListAsync(x => x.TemplateId == request.Id, cancellationToken);
        var tasksQuery = taskProjection.ListAsync(x => x.TemplateId == request.Id, cancellationToken);
        
        var stepFieldsQuery = stepFielProjection.ListAsync(x => x.TemplateId == request.Id, cancellationToken);
        var taskFieldsQuery = taskFieldProjection.ListAsync(x => x.TemplateId == request.Id, cancellationToken);

        await Task.WhenAll(stepsQuery, tasksQuery, stepFieldsQuery, taskFieldsQuery);

        var steps = await stepsQuery;
        var tasks = await tasksQuery;

        var stepFields = await stepFieldsQuery;
        var taskFields = await taskFieldsQuery;
        
        var response = new WorkflowTemplateDetailResponse(
            template.Id,
            template.Name,
            template.Description,
            template.TemplateVersion,
            template.Status,
            template.CreatedAt,
            template.UpdatedAt,
            template.SourceTemplateId,
            template.SourceTemplateId.HasValue,
            steps.OrderBy(step => step.Order).Select(step => new WorkflowTemplateStepDetailResponse(
                step.Id,
                step.Title,
                step.Description,
                step.Order,
                stepFields.Where(field => field.StepId == step.Id)
                    .OrderBy(field => field.Order)
                    .Select(field => new WorkflowTemplateStepFieldDetailResponse(
                        field.Id,
                        field.Label,
                        field.FieldType,
                        field.Options,
                        field.Order
                    )).ToList(),
                tasks.Where(task => task.StepId == step.Id)
                    .OrderBy(task => task.Order)
                    .Select(task => new WorkflowTemplateStepTaskDetailResponse(
                        task.Id,
                        task.Title,
                        task.Description,
                        task.Type,
                        task.Configuration,
                        task.Order,
                        task.IsOptional,
                        taskFields.Where(field => field.TaskId == task.Id)
                            .OrderBy(field => field.Order)
                            .Select(field => new WorkflowTemplateStepTaskFieldDetailResponse(
                            field.Id,
                            field.Label,
                            field.FieldType,
                            field.Options,
                            field.Order
                        )).ToList()
                    )).ToList()
            )).ToList()
        );

        return Result.Success(response);
    }
}
