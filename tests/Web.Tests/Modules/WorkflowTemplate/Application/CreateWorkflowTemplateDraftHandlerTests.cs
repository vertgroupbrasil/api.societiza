using Moq;
using Core.Shared.Results;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.Template;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application;

public class CreateWorkflowTemplateDraftHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsExistingDraft_WhenOneAlreadyExists()
    {
        var templateId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        var applicationService = new Mock<IWorkflowTemplateApplicationService>(MockBehavior.Strict);
        var projection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>>();

        projection
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectionModel.WorkflowTemplate(
                draftId,
                "Draft",
                "Descricao",
                "1.0",
                "Draft",
                DateTimeOffset.UtcNow,
                null,
                templateId));

        var handler = new CreateWorkflowTemplateDraftHandler(applicationService.Object, projection.Object);

        var result = await handler.Handle(new CreateWorkflowTemplateDraftCommand(templateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(draftId, result.Value.Id);
        applicationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenSourceContainsDuplicatedTitles_ClonesSuccessfully()
    {
        var templateId = Guid.NewGuid();
        var source = WorkflowTemplateAggregate.Create("Template", "Descricao");
        source.TryDequeueEvent(out _);

        var firstStepId = source.AddStep("Etapa 1", "Descricao", 1);
        var secondStepId = source.AddStep("Etapa 2", "Descricao", 2);
        source.TryDequeueEvent(out _);
        source.TryDequeueEvent(out _);

        source.UpdateStep(secondStepId, "Etapa 1", "Descricao repetida", 2);
        source.TryDequeueEvent(out _);

        _ = source.AddTask(firstStepId, "Tarefa 1", "Descricao", TaskType.Manual, string.Empty, 1, false);
        var secondTaskId = source.AddTask(firstStepId, "Tarefa 2", "Descricao", TaskType.Manual, string.Empty, 2, false);
        source.TryDequeueEvent(out _);
        source.TryDequeueEvent(out _);

        source.UpdateTask(firstStepId, secondTaskId, "Tarefa 1", "Descricao repetida", TaskType.Manual, string.Empty, 2, false);
        source.TryDequeueEvent(out _);

        var applicationService = new Mock<IWorkflowTemplateApplicationService>();
        var projection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>>();

        projection
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionModel.WorkflowTemplate?)null);

        applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(source));

        applicationService
            .Setup(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateWorkflowTemplateDraftHandler(applicationService.Object, projection.Object);

        var result = await handler.Handle(new CreateWorkflowTemplateDraftCommand(templateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        applicationService.Verify(
            x => x.AppendEventsAsync(
                It.Is<WorkflowTemplateAggregate>(draft =>
                    draft.SourceTemplateId == source.Id &&
                    draft.Steps.Count == 2 &&
                    draft.Steps.Count(step => step.Title == "Etapa 1") == 2 &&
                    draft.Steps.Single(step => step.Order == 1).Tasks.Count(task => task.Title == "Tarefa 1") == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
